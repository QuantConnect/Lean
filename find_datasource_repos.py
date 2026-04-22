#!/usr/bin/env python3
"""
Discover QuantConnect/Lean.DataSource.* repositories (non-archived) that define
the data types listed in the alternative-data dump JSON, and print them as a
semicolon-separated list on stdout.

Intended for CI: the list is captured and exported as the $ADDITIONAL_STUBS_REPOS
environment variable that ci_build_stubs.sh reads on its line 23.
"""

import json
import os
import re
import subprocess
import sys
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

JSON_URL = "https://s3.amazonaws.com/cdn.quantconnect.com/web/docs/alternative-data-dump-v2024-01-02.json"
OWNER = "QuantConnect"
REPO_PREFIX = "Lean.DataSource."


def log(msg: str) -> None:
    print(msg, file=sys.stderr, flush=True)


def run(cmd: list[str]) -> str:
    """Run a command and return its stdout. Raises on non-zero exit."""
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"{' '.join(cmd)} failed: {result.stderr.strip()}")
    return result.stdout


def fetch_data_types() -> set[str]:
    """Download the alt-data dump and extract all QuantConnect.DataSource.* types
    referenced from the 'Data Point Attributes' doc sections."""
    log(f"Downloading {JSON_URL} ...")
    with urllib.request.urlopen(JSON_URL) as r:
        data = json.loads(r.read())

    pat = re.compile(r'data-tree="QuantConnect\.DataSource\.([^"]+)"')
    types: set[str] = set()
    for item in data:
        for doc in item.get("documentation", []):
            if isinstance(doc, dict) and doc.get("title") == "Data Point Attributes":
                for m in pat.findall(doc.get("content", "")):
                    types.add(m)
    log(f"Extracted {len(types)} data types from 'Data Point Attributes'")
    return types


def list_candidate_repos() -> dict[str, dict]:
    """Return {repo_name: {pushedAt, branch}} for all non-archived
    Lean.DataSource.* repos. Fetches everything we need in one API call; the
    default branch comes from defaultBranchRef so we avoid a per-repo lookup."""
    out = run([
        "gh", "repo", "list", OWNER,
        "--limit", "1000",
        "--json", "name,isArchived,pushedAt,defaultBranchRef",
    ])
    repos: dict[str, dict] = {}
    for r in json.loads(out):
        if not r["name"].startswith(REPO_PREFIX) or r["isArchived"]:
            continue
        branch_ref = r.get("defaultBranchRef") or {}
        repos[r["name"]] = {
            "pushedAt": r["pushedAt"],
            "branch": branch_ref.get("name", "master"),
        }
    log(f"Found {len(repos)} non-archived {REPO_PREFIX}* repos")
    return repos


def list_cs_files(repo: str, branch: str) -> list[str]:
    """Return every .cs file path in the repo (recursive tree listing)."""
    out = run([
        "gh", "api",
        f"repos/{OWNER}/{repo}/git/trees/{branch}?recursive=1",
        "--jq", '.tree[] | select(.path | endswith(".cs")) | .path',
    ])
    return [line for line in out.splitlines() if line]


def search_class_definition(data_type: str) -> set[str]:
    """Fallback: use GitHub code search to find repos that define the type.
    Searches for 'class|struct|enum|interface|record DataType' in .cs files."""
    found: set[str] = set()
    short = data_type.split(".")[-1]
    for kw in ("class", "struct", "enum", "interface", "record"):
        try:
            out = run([
                "gh", "search", "code", f"{kw} {short}",
                "--owner", OWNER, "--extension", "cs",
                "--limit", "30",
                "--json", "repository,path,textMatches",
            ])
        except RuntimeError:
            continue
        try:
            hits = json.loads(out)
        except json.JSONDecodeError:
            continue
        for hit in hits:
            name = hit["repository"]["nameWithOwner"].split("/")[-1]
            text = " ".join(
                frag.get("fragment", "") for frag in hit.get("textMatches", [])
            )
            if re.search(rf"\b{kw}\s+{re.escape(short)}\b", text):
                found.add(name)
    return found


def build_repo_to_files(repos: dict[str, dict]) -> dict[str, list[str]]:
    """Fetch the .cs file list for every repo in parallel."""
    repo_to_files: dict[str, list[str]] = {}
    total = len(repos)

    def _fetch(repo: str) -> tuple[str, list[str]]:
        try:
            return repo, list_cs_files(repo, repos[repo]["branch"])
        except RuntimeError as e:
            log(f"    warning on {repo}: {e}")
            return repo, []

    with ThreadPoolExecutor(max_workers=16) as pool:
        futures = {pool.submit(_fetch, r): r for r in repos}
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
    Primary match: a file in the repo is named <DataType>.cs (basename).
    Dotted types (e.g., EODHD.Events) are nested types resolvable via a sibling
    non-dotted type in the same repo, so they are skipped here."""
    type_to_repos: dict[str, set[str]] = {}
    unmatched: set[str] = set()

    for dt in sorted(data_types):
        if "." in dt:
            continue
        filename = f"{dt}.cs"
        hits = {
            repo
            for repo, files in repo_to_files.items()
            if any(Path(f).name == filename for f in files)
        }
        if hits:
            type_to_repos[dt] = hits
        else:
            unmatched.add(dt)

    return type_to_repos, unmatched


def main() -> int:
    data_types = fetch_data_types()
    repos = list_candidate_repos()
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

    # If a type is defined in more than one repo, keep only the one with the
    # latest push. Example: Estimize* types live in both Lean.DataSource.Estimize
    # and Lean.DataSource.ExtractAlpha; the latter is the more recent fork, so
    # it wins and Estimize drops out of the final list.
    def pushed_at(repo: str) -> str:
        return repos.get(repo, {}).get("pushedAt", "")

    picked: dict[str, str] = {
        dt: max(hits, key=pushed_at)
        for dt, hits in type_to_repos.items()
    }

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
    matching_urls: set[str] = {
        f"https://github.com/{OWNER}/{name}" for name in picked.values()
    }
    log(f"\n{len(matching_urls)} unique repos with matching data types")

    # Merge in any extra repos carried via the ADDITIONAL_STUBS_REPOS env var
    # (CI secret) so private/out-of-band repos still make it into the build.
    extra = os.environ.get("ADDITIONAL_STUBS_REPOS", "").strip()
    if extra:
        extras = {r.strip() for r in extra.split(";") if r.strip()}
        log(f"Merging {len(extras)} extra repos from $ADDITIONAL_STUBS_REPOS")
        matching_urls |= extras

    # stdout: semicolon-separated list consumed by ci_build_stubs.sh
    print(";".join(sorted(matching_urls)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
