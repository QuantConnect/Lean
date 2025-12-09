#!/usr/bin/env python3
"""
Promote Learning Script
=======================
Promotes a learning record from trusted=false to trusted=true.

REQUIREMENTS:
- CI tests must have passed
- Human reviewer must have approved in PR comments
- This script is run MANUALLY after human approval

USAGE:
    python scripts/promote_learning.py <learning_id>
    python scripts/promote_learning.py a1b2c3d4-6666-4000-8000-000000000006

This script:
1. Finds the learning by ID
2. Asks for confirmation
3. Updates trusted=false to trusted=true
4. Adds promotion timestamp and promoter to metadata
"""

import json
import sys
import os
from datetime import datetime

JSONL_FILE = "solutions_learned.jsonl"


def load_learnings():
    """Load all learnings from JSONL file."""
    learnings = []
    with open(JSONL_FILE, 'r') as f:
        for line in f:
            if line.strip():
                learnings.append(json.loads(line))
    return learnings


def save_learnings(learnings):
    """Save all learnings back to JSONL file."""
    with open(JSONL_FILE, 'w') as f:
        for learning in learnings:
            f.write(json.dumps(learning) + '\n')


def find_learning(learnings, learning_id):
    """Find a learning by ID."""
    for i, learning in enumerate(learnings):
        if learning.get('id') == learning_id:
            return i, learning
    return None, None


def promote(learning_id, promoter="manual"):
    """Promote a learning to trusted=true."""
    learnings = load_learnings()
    idx, learning = find_learning(learnings, learning_id)

    if learning is None:
        print(f"ERROR: Learning with ID '{learning_id}' not found.")
        print("\nAvailable learnings:")
        for l in learnings:
            status = "TRUSTED" if l['metadata'].get('trusted') else "untrusted"
            print(f"  {l['id']}: {l['topic']} [{status}]")
        return False

    if learning['metadata'].get('trusted'):
        print(f"Learning '{learning['topic']}' is already trusted.")
        return True

    # Show learning details
    print("=" * 60)
    print("LEARNING TO PROMOTE")
    print("=" * 60)
    print(f"ID:      {learning['id']}")
    print(f"Topic:   {learning['topic']}")
    print(f"Type:    {learning['type']}")
    print(f"Error:   {learning['error_signature']}")
    print(f"Approved by: {learning['metadata'].get('approved_by', 'unknown')}")
    print("")
    print("Canonical format/workflow:")
    print(f"  {learning['canonical']}")
    print("")

    # Confirm
    confirm = input("Promote this learning to trusted=true? (yes/no): ")
    if confirm.lower() != 'yes':
        print("Aborted.")
        return False

    # Promote
    learning['metadata']['trusted'] = True
    learning['metadata']['promoted_at'] = datetime.utcnow().isoformat() + 'Z'
    learning['metadata']['promoted_by'] = promoter

    learnings[idx] = learning
    save_learnings(learnings)

    print("")
    print(f"SUCCESS: Learning '{learning['topic']}' promoted to trusted.")
    return True


def list_learnings():
    """List all learnings with their status."""
    learnings = load_learnings()

    print("=" * 60)
    print("ALL LEARNINGS")
    print("=" * 60)

    for learning in learnings:
        status = "TRUSTED" if learning['metadata'].get('trusted') else "untrusted"
        print(f"\n[{status}] {learning['topic']}")
        print(f"  ID: {learning['id']}")
        print(f"  Error signature: {learning['error_signature']}")
        print(f"  Approved by: {learning['metadata'].get('approved_by', 'unknown')}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python scripts/promote_learning.py <learning_id>")
        print("       python scripts/promote_learning.py --list")
        sys.exit(1)

    if sys.argv[1] == '--list':
        list_learnings()
    else:
        learning_id = sys.argv[1]
        promoter = sys.argv[2] if len(sys.argv) > 2 else os.environ.get('USER', 'manual')
        success = promote(learning_id, promoter)
        sys.exit(0 if success else 1)
