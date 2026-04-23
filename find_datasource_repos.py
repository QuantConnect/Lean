#!/usr/bin/env python3
"""
Discover QuantConnect/Lean.DataSource.* repositories (non-archived) that define
the data types listed in the alternative-data dump JSON, and print them as a
semicolon-separated list on stdout.

Intended for CI: the output is captured and exported as $ADDITIONAL_STUBS_REPOS
so ci_build_stubs.sh can read it on its line 23.

Auth: REST API via urllib, reading QC_GIT_TOKEN (same env var ci_build_stubs.sh
uses when embedding the token into git clone URLs). Only 'repo' scope is
required, unlike the 'read:org' that gh's GraphQL path demanded.
"""

import json
import os
import re
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from http.client import HTTPException, HTTPMessage, HTTPSConnection
from pathlib import Path
from threading import local
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import urlopen

JSON_URL = "https://s3.amazonaws.com/cdn.quantconnect.com/web/docs/alternative-data-dump-v2024-01-02.json"
OWNER = "QuantConnect"
REPO_PREFIX = "Lean.DataSource."
GH_HOST = "api.github.com"

_tls = local()


def log(msg: str) -> None:
    print(msg, file=sys.stderr, flush=True)


def _conn() -> HTTPSConnection:
    """One persistent HTTPS connection per thread so repeat requests share
    the TLS handshake (saves ~300-500ms per reused request)."""
    conn = getattr(_tls, "conn", None)
    if conn is None:
        conn = HTTPSConnection(GH_HOST, timeout=30)
        _tls.conn = conn
    return conn


def _request(path: str, params: dict | None = None) -> tuple[object, dict[str, str]]:
    """GET a GitHub REST API path. Returns (parsed JSON, response headers)."""
    url = path + ("?" + urlencode(params) if params else "")
    token = os.environ.get("QC_GIT_TOKEN")
    headers = {
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "find-datasource-repos",
        "Connection": "keep-alive",
        **({"Authorization": f"Bearer {token}"} if token else {}),
    }

    def _once() -> tuple[int, bytes, dict[str, str]]:
        conn = _conn()
        conn.request("GET", url, headers=headers)
        resp = conn.getresponse()
        return resp.status, resp.read(), dict(resp.getheaders())

    try:
        status, raw, resp_headers = _once()
    except (HTTPException, ConnectionError, OSError):
        # Stale connection; reset and retry once.
        try:
            _conn().close()
        finally:
            _tls.conn = None
        status, raw, resp_headers = _once()

    if status >= 400:
        msg = HTTPMessage()
        for k, v in resp_headers.items():
            msg[k] = v
        raise HTTPError(url, status, f"HTTP {status}", msg, None)
    return json.loads(raw), resp_headers


def _paginate(path: str, params: dict) -> list:
    """Follow REST pagination via the Link header."""
    items: list = []
    page = 1
    while True:
        body, headers = _request(path, {**params, "per_page": 100, "page": page})
        assert isinstance(body, list), f"{path} did not return a list"
        items.extend(body)
        if 'rel="next"' not in headers.get("Link", ""):
            return items
        page += 1


def fetch_data_types() -> set[str]:
    log(f"Downloading {JSON_URL} ...")
    with urlopen(JSON_URL) as r:
        data = json.loads(r.read())
    pat = re.compile(r'data-tree="QuantConnect\.DataSource\.([^"]+)"')
    types: set[str] = set()
    for item in data:
        for doc in item.get("documentation", []):
            if isinstance(doc, dict) and doc.get("title") == "Data Point Attributes":
                types.update(pat.findall(doc.get("content", "")))
    log(f"Extracted {len(types)} data types from 'Data Point Attributes'")
    return types


def list_candidate_repos() -> dict[str, dict]:
    """Return {repo_name: {pushedAt, branch}} for all non-archived
    Lean.DataSource.* repos via GET /orgs/{org}/repos."""
    raw = _paginate(f"/orgs/{OWNER}/repos", {"type": "all"})
    repos = {
        r["name"]: {"pushedAt": r["pushed_at"], "branch": r["default_branch"]}
        for r in raw
        if r["name"].startswith(REPO_PREFIX) and not r.get("archived")
    }
    log(f"Found {len(repos)} non-archived {REPO_PREFIX}* repos")
    return repos


def list_cs_files(repo: str, branch: str) -> list[str]:
    """Every .cs file path in the repo (recursive tree listing)."""
    body, _ = _request(f"/repos/{OWNER}/{repo}/git/trees/{branch}", {"recursive": "1"})
    assert isinstance(body, dict)
    return [e["path"] for e in body.get("tree", []) if e.get("path", "").endswith(".cs")]


def search_class_definition(data_type: str) -> set[str]:
    """Fallback: REST code search for '<kw> <DataType>' in the org's .cs files."""
    found: set[str] = set()
    short = data_type.split(".")[-1]
    for kw in ("class", "struct", "enum", "interface", "record"):
        q = f'"{kw} {short}" user:{OWNER} extension:cs'
        try:
            body, _ = _request("/search/code", {"q": q, "per_page": 30})
        except HTTPError as e:
            log(f"    search '{kw} {short}' failed: HTTP {e.code}")
            continue
        assert isinstance(body, dict)
        for item in body.get("items", []):
            found.add(item["repository"]["full_name"].split("/")[-1])
    return found


def build_repo_to_files(repos: dict[str, dict]) -> dict[str, list[str]]:
    """Fetch the .cs file list for every repo in parallel."""
    def _fetch(repo: str) -> tuple[str, list[str]]:
        try:
            return repo, list_cs_files(repo, repos[repo]["branch"])
        except Exception as e:
            log(f"    warning on {repo}: {e}")
            return repo, []

    repo_to_files: dict[str, list[str]] = {}
    total = len(repos)
    with ThreadPoolExecutor(max_workers=32) as pool:
        futures = [pool.submit(_fetch, r) for r in repos]
        for i, fut in enumerate(as_completed(futures), 1):
            repo, files = fut.result()
            repo_to_files[repo] = files
            log(f"  [{i}/{total}] {repo} ({len(files)} .cs files)")
    return repo_to_files


def match_types_to_repos(
    data_types: set[str],
    repo_to_files: dict[str, list[str]],
) -> tuple[dict[str, set[str]], set[str]]:
    """Return (type -> matching repos, set of unmatched types).
    Primary match: a file named <DataType>.cs. Dotted types (e.g. EODHD.Events)
    are nested inside outer types that get matched via a sibling non-dotted type
    in the same repo, so they are skipped here."""
    repo_to_basenames = {
        repo: {Path(f).name for f in files} for repo, files in repo_to_files.items()
    }
    type_to_repos: dict[str, set[str]] = {}
    unmatched: set[str] = set()
    for dt in sorted(data_types):
        if "." in dt:
            continue
        filename = f"{dt}.cs"
        hits = {repo for repo, bases in repo_to_basenames.items() if filename in bases}
        if hits:
            type_to_repos[dt] = hits
        else:
            unmatched.add(dt)
    return type_to_repos, unmatched


def main() -> int:
    # Independent network calls run concurrently: 2.5MB JSON from S3 and
    # GET /orgs/QuantConnect/repos.
    with ThreadPoolExecutor(max_workers=2) as pool:
        f_types = pool.submit(fetch_data_types)
        f_repos = pool.submit(list_candidate_repos)
        data_types = f_types.result()
        repos = f_repos.result()
    repo_to_files = build_repo_to_files(repos)
    type_to_repos, unmatched = match_types_to_repos(data_types, repo_to_files)

    if unmatched:
        log(f"\n{len(unmatched)} types without filename match; "
            "falling back to code search:")
        valid = set(repos)
        for dt in sorted(unmatched):
            log(f"  searching '{dt}' ...")
            hits = search_class_definition(dt) & valid
            if hits:
                type_to_repos[dt] = hits
            else:
                log(f"    (still no match for {dt})")

    # When a type is defined in more than one repo, keep the one with the
    # latest push. Example: Estimize* types live in both Lean.DataSource.Estimize
    # and Lean.DataSource.ExtractAlpha; the latter is the more recent fork.
    pushed_at = lambda r: repos.get(r, {}).get("pushedAt", "")  # noqa: E731
    picked = {dt: max(hits, key=pushed_at) for dt, hits in type_to_repos.items()}

    log("\n=== Summary ===")
    for dt in sorted(type_to_repos):
        hits = type_to_repos[dt]
        winner = picked[dt]
        if len(hits) > 1:
            losers = sorted(hits - {winner})
            log(f"  {dt:45s} -> {winner}  (over: {', '.join(losers)})")
        else:
            log(f"  {dt:45s} -> {winner}")

    # ci_build_stubs.sh line 36 does ${REPO//github.com/"${TOKEN}@github.com"}
    # so the clone URL must contain "github.com" -- emit full URLs, not bare names.
    matching_urls = {f"https://github.com/{OWNER}/{name}" for name in picked.values()}
    log(f"\n{len(matching_urls)} unique repos with matching data types")

    print(";".join(sorted(matching_urls)))
    if not os.environ.get("QC_GIT_TOKEN"):
        log("QC_GIT_TOKEN env is empty")
    return 0


if __name__ == "__main__":
    sys.exit(main())
