from pathlib import Path
import sys

ENGINE_DIR = Path(__file__).resolve().parent / "agentic_loop"
if str(ENGINE_DIR) not in sys.path:
    sys.path.insert(0, str(ENGINE_DIR))

from main import main

if __name__ == "__main__":
    main()
