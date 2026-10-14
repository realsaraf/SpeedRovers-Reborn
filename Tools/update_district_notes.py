from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'PAINTED-GRAPHICS.md'
s=p.read_text(encoding='utf-8-sig')
s=s.replace('Street racing and audio pass — 0.5.1','District, wheel and traffic pass — 0.6.0')
s=s.replace('(three on the coast)','(three on open roads)')
s=s.replace('165 px rider','190 px player rider')
s=s.replace('Repeating 1.8 km route: downtown, outskirts, coast road, old town. Eight building blocks, close urban spacing, open coastal verges, cross streets and zebra crossings.','Repeating 5 km route: downtown (0 m), river bridge (700 m), green outskirts (1,250 m), city day (2,100 m), Italian coast (2,800 m), Tokyo night (3,500 m), and Alps (4,250 m). Projected suspension towers/cables and guardrails, district panoramas, daylight buildings, flower planters, coastal villas, rock faces, conifers, neon shops and cherry trees. Cross streets and zebra crossings occur only in urban sections. These are seven scenery sections in the existing game loop; the original city-unlock menu remains.')
s=s.replace('Eight rivals start ahead.','Two spaced rivals start ahead.')
s=s.replace('distance-driven eight-phase rear tire animation','distance-driven sixteen-phase rear tire animation')
s=s.replace('New art is in Assets/Resources/Painted/Rush*.png.','New art is in Assets/Resources/Painted/Rush*.png, DistrictSkies.png, BridgeStructures.png, DayStreetProps.png, TravelSkies.png and TravelProps.png. District and travel prompts are in Tools/district-art-prompts.json and Tools/travel-art-prompts.json. prepare_district_atlas.py records connected-component bounds without editing generated pixels. The first malformed sky generation was rejected; the second is used.')
s=s.replace('No fighting or crash recovery.','The renderer still uses projected strips and billboards; steep terrain silhouettes, physically accurate light/shadow and fully directional traffic art are not implemented. No fighting or crash recovery.')
p.write_text(s,encoding='utf-8')
p=root/'README.md';s=p.read_text(encoding='utf-8-sig')
s=s.replace('**0.5.0 Street/Rush**','**0.6.0 District/Rush**').replace('Faster perspective, rider packs, dense-to-open districts, intersections, roadworks, oncoming traffic, animated tires and geared engine/police audio.','Seven district environments, sparse traffic, rolling tire sprites, road-contact shadows, bridge structures, coast/Alps scenery, intersections, roadworks, oncoming traffic and geared engine/police audio.')
s=s.replace('**New graphics preview:** source version 0.4.0','**Earlier graphics preview:** version 0.4.0')
s=s.replace('the separate painted preview and current source are `0.4.0`','the separate painted preview is `0.4.0`, and the current Windows Rush source/build is `0.6.0`')
p.write_text(s,encoding='utf-8')
print('Updated graphics scope and version notes')
