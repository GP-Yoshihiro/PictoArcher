#!/usr/bin/env python3
"""Print a compact outline of C# files: types and members with line numbers.

  cs-outline.py Assets/_Project               # whole tree (a project map in a few KB)
  cs-outline.py Assets/_Project/Scripts/Core/FireTimer.cs
  cs-outline.py Assets/_Project --grep Wave    # only members/types matching a word
  cs-outline.py File.cs --all                   # include fields/properties/constants too

Regex-based (no compiler): good enough to decide which line range to Read.
"""
import os
import re
import sys

TYPE = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly)\s+)*(class|struct|enum|interface|record)\s+(\w+)')
MEMBER = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*(public|internal|protected|private)\s+(?:(?:static|override|virtual|abstract|sealed|readonly|async|new|const|event)\s+)*([\w<>\[\],.? ()]+?)\s+(\w+)\s*(\(|\{|=>|=|;)')


def outline(path, word, include_fields):
    out = []
    for no, line in enumerate(open(path, encoding='utf-8', errors='replace'), 1):
        m = TYPE.match(line)
        if m:
            out.append(f"{no:>5} {m.group(1)} {m.group(2)}")
            continue
        m = MEMBER.match(line)
        if m and m.group(3) not in ('if', 'for', 'while', 'switch', 'return'):
            kind = '()' if m.group(4) == '(' else ''
            if not kind and not include_fields:
                continue
            out.append(f"{no:>5}   {m.group(1)[:3]} {m.group(2).strip()} {m.group(3)}{kind}")
    if word:
        out = [l for l in out if word.lower() in l.lower()]
    return out


def main():
    args = sys.argv[1:]
    word = None
    include_fields = '--all' in args
    args = [a for a in args if a != '--all']
    if '--grep' in args:
        i = args.index('--grep'); word = args[i + 1]; del args[i:i + 2]
    root = args[0] if args else '.'
    files = [root] if root.endswith('.cs') else sorted(
        os.path.join(d, f) for d, _, fs in os.walk(root) for f in fs if f.endswith('.cs'))
    for f in files:
        lines = outline(f, word, include_fields)
        if lines:
            n = sum(1 for _ in open(f, encoding='utf-8', errors='replace'))
            print(f"## {f} ({n} lines)")
            print("\n".join(lines))


if __name__ == '__main__':
    main()
