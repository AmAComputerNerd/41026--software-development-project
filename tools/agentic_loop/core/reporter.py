def print_owner_menu(owners: list[str]) -> None:
    print()
    print("=" * 70)
    print("AGENTIC REVIEW MENU")
    for index, owner in enumerate(owners, start=1):
        print(f"{index} - {owner}")
    print(f"{len(owners) + 1} - docker-compose (architecture review)")
    print(f"{len(owners) + 2} - Run All (every owner/layer + compose)")
    print("0 - Exit")
    print("=" * 70)


def print_layer_menu(owner: str, layers: list[tuple[str, str]]) -> None:
    print()
    print("-" * 70)
    print(f"Reviewing: {owner}")
    for index, (_key, layer_label) in enumerate(layers, start=1):
        print(f"{index} - {layer_label}")
    print(f"{len(layers) + 1} - All layers for {owner}")
    print("0 - Back")
    print("-" * 70)


def print_result(title: str, text: str) -> None:
    print()
    print(f"RUNNING: {title}")
    print(text)
