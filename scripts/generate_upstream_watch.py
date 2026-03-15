#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text())


def dump_json(path: Path, data: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + "\n")


def iter_fragment_paths(fragments_dir: Path) -> list[Path]:
    return sorted(path for path in fragments_dir.rglob("*.json") if path.is_file())


def merge_fragments(fragments_dir: Path) -> dict[str, Any]:
    watch_issue_label: str | None = None
    labels: list[dict[str, Any]] = []
    watches: list[dict[str, Any]] = []
    label_names: set[str] = set()
    watch_ids: set[str] = set()

    fragment_paths = iter_fragment_paths(fragments_dir)
    if not fragment_paths:
        raise ValueError(f"No upstream watch fragments found under {fragments_dir}")

    for path in fragment_paths:
        fragment = load_json(path)

        fragment_watch_issue_label = fragment.get("watch_issue_label")
        if fragment_watch_issue_label is not None:
            if watch_issue_label is not None and watch_issue_label != fragment_watch_issue_label:
                raise ValueError(
                    f"Conflicting watch_issue_label values: {watch_issue_label!r} versus {fragment_watch_issue_label!r} in {path}"
                )
            watch_issue_label = fragment_watch_issue_label

        for label in fragment.get("labels", []):
            name = label.get("name")
            if not name:
                raise ValueError(f"Label without name in {path}")
            if name in label_names:
                raise ValueError(f"Duplicate label name {name!r} in {path}")
            label_names.add(name)
            labels.append(label)

        for watch in fragment.get("watches", []):
            watch_id = watch.get("id")
            if not watch_id:
                raise ValueError(f"Watch without id in {path}")
            if watch_id in watch_ids:
                raise ValueError(f"Duplicate watch id {watch_id!r} in {path}")
            watch_ids.add(watch_id)
            watches.append(watch)

    if watch_issue_label is None:
        raise ValueError("No watch_issue_label defined in upstream watch fragments")

    return {
        "watch_issue_label": watch_issue_label,
        "labels": labels,
        "watches": watches,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate .github/upstream-watch.json from fragment files.")
    parser.add_argument("--fragments-dir", default=".github/upstream-watch.d")
    parser.add_argument("--output", default=".github/upstream-watch.json")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    fragments_dir = Path(args.fragments_dir)
    output_path = Path(args.output)
    generated = merge_fragments(fragments_dir)
    rendered = json.dumps(generated, indent=2) + "\n"

    if args.check:
        current = output_path.read_text() if output_path.exists() else ""
        if current != rendered:
            print(f"{output_path} is out of date.", file=sys.stderr)
            return 1
        return 0

    dump_json(output_path, generated)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
