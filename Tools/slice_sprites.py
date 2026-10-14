"""Split original Speed Rovers sprite sheets into Unity-friendly transparent frames."""
from pathlib import Path
from PIL import Image

project = Path(__file__).resolve().parents[1]
original = project / "Assets" / "Resources" / "Original" / "Game"
output = project / "Assets" / "Resources" / "Slices"

for name in ("Michalangelo", "Monalisa", "Faraday", "Leonardo", "Ada"):
    for animation, rows, folder in (("Speed", 5, "Riders"), ("Slow", 3, "RidersSlow")):
        source = Image.open(original / "Characters" / f"{name}{animation}.png").convert("RGBA")
        target = output / folder / name
        target.mkdir(parents=True, exist_ok=True)
        for row in range(rows):
            for column in range(5):
                source.crop((column * 168, row * 263, (column + 1) * 168, (row + 1) * 263)).save(
                    target / f"{column}_{row}.png"
                )

for city in ("NewYork", "Mexico", "Shanghai", "London", "Tokyo"):
    source = Image.open(original / "Sprites" / "Vehicles" / f"{city}Vehicles.png").convert("RGBA")
    target = output / "Traffic" / city
    target.mkdir(parents=True, exist_ok=True)
    for column in range(4):
        source.crop((column * 167, 0, (column + 1) * 167, 241)).save(target / f"{column}.png")

    for side, filename in enumerate(("buildings", "buildingsreverse")):
        source = Image.open(original / "Sprites" / city / f"{filename}.png").convert("RGBA")
        columns, rows = (2, 3) if city == "Shanghai" else (3, 2)
        target = output / "Buildings" / city / str(side)
        target.mkdir(parents=True, exist_ok=True)
        for row in range(rows):
            for column in range(columns):
                index = row * columns + column
                left = round(column * source.width / columns)
                top = round(row * source.height / rows)
                right = round((column + 1) * source.width / columns)
                bottom = round((row + 1) * source.height / rows)
                frame = source.crop((left, top, right, bottom))
                bounds = frame.getbbox()
                if bounds:
                    frame.crop(bounds).save(target / f"{index}.png")

print(f"Created rider, traffic and roadside sprites in {output}")
