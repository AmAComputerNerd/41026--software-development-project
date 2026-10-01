import os
from pathlib import Path
import sys

ENGINE_DIR = Path(__file__).resolve().parent / "agentic_loop"
if str(ENGINE_DIR) not in sys.path:
    sys.path.insert(0, str(ENGINE_DIR))

# Auto-detect existing virtual environment if dependencies are missing
try:
    import requests
    import dotenv
    import yaml
except ImportError:
    venv_dir = ENGINE_DIR / ".venv"
    venv_bin = venv_dir / ("Scripts" if sys.platform == "win32" else "bin")
    venv_python = venv_bin / ("python.exe" if sys.platform == "win32" else "python")
    if venv_python.exists() and Path(sys.prefix).resolve() != venv_dir.resolve():
        os.execv(str(venv_python), [str(venv_python)] + sys.argv)
    else:
        print(
            "Error: Required dependencies (requests, python-dotenv, pyyaml, openai) are missing.\n"
            "Please activate the virtual environment or install them:\n"
            f"  cd {ENGINE_DIR}\n"
            "  python3 -m venv .venv\n"
            "  source .venv/bin/activate  # on Windows: .venv\\Scripts\\activate\n"
            "  pip install -r requirements.txt",
            file=sys.stderr,
        )
        sys.exit(1)

from main import main

if __name__ == "__main__":
    main()
