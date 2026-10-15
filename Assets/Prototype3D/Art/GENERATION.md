# Prototype art generation

Generated with the built-in imagegen tool. These are runtime assets, not mock gameplay screenshots.

## Cockpit.png

Create a production-ready photorealistic game sprite: first-person sport motorcycle cockpit viewed from rider eye level, looking forward over handlebars. Wide landscape 16:9 canvas, TRUE transparent alpha background. ONLY motorcycle cockpit and rider's gloved hands and jacket forearms; no road, sky, scenery, ground, rectangle background or text UI. Composition: entire upper 50% of canvas completely transparent; cockpit occupies lower half, with black angular mirrors rising to 45% from top on left and right, realistic handlebars, brake lever left and throttle right, two black armored gloves holding grips, detailed black textile jacket forearms entering from bottom corners, deep petrol-blue tank partly cropped at bottom center, carbon fiber trim, machined silver bolts, wiring, believable switches. Compact dark blank digital dashboard centered around 72% from top; leave dashboard screen dark without any numbers so game can render live data. Mirrors subtly reflect soft pale blue sky only inside mirror glass. Premium modern racing game realism: meticulous material texture, subtle wear, restrained highlights, warm afternoon sun from upper left, shadows consistent. Symmetrical forward-facing perspective, no tilt, no logos, no branding, no lettering. Full motorcycle assembly fits inside canvas left/right edges; forearms and tank continue naturally out bottom edge. This is a transparent compositing asset, not a screenshot.

## Facades.png

Production game texture, photorealistic architectural facade atlas. Wide 2:1 image divided into FOUR equal vertical rectangular panels edge-to-edge, no gaps or borders. Each panel is a completely flat orthographic front elevation facade of a different elegant six-story city building, from pavement base at bottom to flat roof cornice at top. Panel1 weathered cream limestone Parisian apartments, dark iron balcony railings, ground-floor boutique glazing and dark green fabric awning. Panel2 warm red brick classic urban apartment building, stone window lintels, black framed windows, ground-floor cafe glazing. Panel3 gray limestone art deco facade with tall windows, bronze frames and refined entrance. Panel4 warm sandstone European building with balconies and ground floor shop windows. Every building is exactly six floors including ground floor. Crisp realistic window reflections, individual brick or stone surface detail, subtle dirt streaks and crevices, believable scale, no excessive repetition within each panel. Flat diffuse daylight illumination with soft ambient occlusion under cornices, no strong directional shadows. IMPORTANT: parallel straight vertical and horizontal lines, ZERO perspective, no visible side walls or roof surfaces, no sky, no people, no cars, no street, no trees, no text, no logos. Facade pixels fill the whole canvas. This image will be UV mapped onto real 3D building facades, not used as a billboard scene.

## DaySky.hdr
Photographed HDRI: Kloofendal 48d Partly Cloudy (Pure Sky), Greg Zaal / Jarod Guest, Poly Haven, CC0.
Source: https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky
Downloaded 1K HDR for prototype use. Existing DayTree.png and CityAsphalt.png reused from the game.


## Grand Tour revision (2026-10-14)
Built-in image generation, production runtime textures.

Cockpit edit: Remove only the grey curved windscreen above the dashboard, including rim and reflections. Replace with full alpha transparency. Keep same dimensions, dashboard location, mirrors, handlebars, gloves, tank, detail, framing and all other elements. No background, scenery, replacement glass or tint. Preserve transparency.

Facade replacement: Four equal vertical strips in a square atlas. Flat orthographic modern city facades: charcoal steel and blue-grey curtain-wall glass; warm buff limestone and black windows; red-brown brick residences; dark granite and golden office windows. Twelve storeys, four bays, parallel lines, no perspective, roofs, ground, people, signs. Fine material detail, subtle dirt, diffuse reflections and neutral lighting.

GrandTour/RockDiffuse.png: Seamless photorealistic mountain rock albedo. Grey and warm brown weathered sedimentary stone, strata, cracks, gravel, sparse moss. Orthographic evenly diffuse neutral light, no horizon or objects, no directional shadows or highlights. Fine multiscale variation and restrained contrast.

Generation IDs: cockpit exec-606c594d-2f36-402f-848f-a5909ac1cf2c; facades exec-d23c3c13-51c9-471b-a724-2f11ce4749e0; rock exec-93758b2d-5051-43cf-990c-c3e70d24039a.

Pine.png: One mature photorealistic alpine spruce, full trunk base and pointed top, irregular layered branches and fine needles, dark olive/forest greens, grey-brown bark, diffuse daylight, upright ground-eye-level cutout. True alpha, transparent branch gaps, no ground/scenery/text/shadow plane/fringe. Generated exec-2b04d42e-5d92-4689-aa06-179c5642099b.
