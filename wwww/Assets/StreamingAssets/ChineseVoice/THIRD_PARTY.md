# Bundled Mandarin speech worker

Fixed WAV recordings play without this worker or a network connection.
New dialogue on Windows uses `neural-worker.py` and the portable runtime.

- CPython 3.12.14: Python Software Foundation license, included as `LICENSE.txt` inside `neural-runtime.zip`. Source: https://www.python.org/downloads/release/python-31214/
- edge-tts 7.3.1: LGPL-3.0; its Python source and distribution license are included under `Lib/site-packages` in the archive. Source: https://github.com/rany2/edge-tts
- Dependency sources and distribution license files are included under `Lib/site-packages`.

`Tools/build-neural-runtime.py` rebuilds the archive from the author's Python environment. The editable worker and `delivery.json` are separate from the archive; the version hash causes modified runtime archives to extract into a separate cache directory. Neural synthesis uses an online Microsoft speech service through edge-tts; availability is external to the game. Offline new sentences fall back to an installed Windows Mandarin voice. This does not reproduce a real person's voice.
