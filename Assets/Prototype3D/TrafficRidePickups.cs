using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class TrafficRidePrototype
{
    const float FirstFuelDistance = 5200f;
    const float FuelSpacing = 5600f;
    const float FuelFinishClearance = 1500f;
    const float FuelTankSeconds = 110f;
    const float FuelRefill = .30f;
    sealed class Pickup
    {
        public Transform root, marker;
        public float at, x;
        public bool taken;
    }
    readonly List<Pickup> pickups = new List<Pickup>();
    float nextFuel = FirstFuelDistance;
    int fuelStopsTaken;
    Material fuelBeaconMaterial, fuelGlowMaterial;
    Mesh fuelArrowMesh;

    static float FuelLane(int stop)
    {
        // Authored forward-lane sequence: alternating service points, then an easier final stop.
        // No random repositioning while the rider approaches, and none in oncoming traffic.
        return stop % 5 == 1 || stop % 5 == 3 ? 5.4f : 1.85f;
    }

    void PlaceNextPickup(Pickup p)
    {
        int stop = Mathf.RoundToInt((nextFuel - FirstFuelDistance) / FuelSpacing);
        p.at = nextFuel;
        p.x = FuelLane(stop);
        p.taken = false;
        nextFuel += FuelSpacing;
    }

    void CreateFuelPickup()
    {
        var p = new Pickup { root = new GameObject("Fuel service pickup").transform };
        var body = new GameObject("Grounded steel fuel can").transform;
        body.SetParent(p.root, false);
        body.localPosition = new Vector3(0, .39f, 0);
        var red = Mat("Fuel can enamel", new Color(.9f, .12f, .04f), .35f, .6f);
        Box("Steel body", Vector3.zero, new Vector3(.6f, .75f, .3f), red, body);
        Box("Fuel label", new Vector3(0, 0, -.16f), new Vector3(.32f, .36f, .02f), white, body);
        Bar("Handle top", new Vector3(-.18f, .58f, 0), new Vector3(.13f, .58f, 0), .045f, red, body);
        Bar("Handle left", new Vector3(-.18f, .36f, 0), new Vector3(-.18f, .58f, 0), .045f, red, body);
        Bar("Handle right", new Vector3(.13f, .36f, 0), new Vector3(.13f, .58f, 0), .045f, red, body);
        Box("Pouring neck", new Vector3(.23f, .43f, 0), new Vector3(.14f, .17f, .18f), metal, body);
        Box("Cap", new Vector3(.23f, .53f, 0), new Vector3(.18f, .05f, .21f), rubber, body);
        Bar("Pressed steel diagonal", new Vector3(-.24f, -.31f, -.17f), new Vector3(.24f, .31f, -.17f), .018f, red, body);
        Bar("Pressed steel diagonal", new Vector3(.24f, -.31f, -.17f), new Vector3(-.24f, .31f, -.17f), .018f, red, body);
        var label = new GameObject("FUEL label");
        label.transform.SetParent(body, false);
        label.transform.localPosition = new Vector3(0, 0, -.185f);
        label.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var text = label.AddComponent<TextMesh>();
        text.text = "FUEL"; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
        text.fontSize = 48; text.characterSize = .06f; text.color = new Color(.12f, .14f, .16f);

        if (fuelArrowMesh == null) CreateFuelBeaconAssets();
        p.marker = new GameObject("Spinning fuel guidance arrow").transform;
        p.marker.SetParent(p.root, false);
        // Crossed, extruded blades retain a visible silhouette throughout rotation.
        for (int i = 0; i < 2; i++)
        {
            AddFuelArrowBlade(p.marker, i * 90f, 1, fuelBeaconMaterial, "Luminous arrow");
            if (fuelGlowMaterial != null) AddFuelArrowBlade(p.marker, i * 90f, 1.15f, fuelGlowMaterial, "Arrow glow");
        }
        pickups.Add(p);
    }

    void CreateFuelBeaconAssets()
    {
        fuelBeaconMaterial = Mat("Fuel beacon turquoise", new Color(.12f, 1, .77f), .1f, .45f);
        fuelBeaconMaterial.EnableKeyword("_EMISSION");
        fuelBeaconMaterial.SetColor("_EmissionColor", new Color(.06f, 1, .58f) * 2.4f);
        var glowShader = Resources.Load<Shader>("GrandTour/FuelBeacon");
        if (glowShader != null)
        {
            fuelGlowMaterial = new Material(glowShader) { name = "Fuel beacon soft glow" };
            fuelGlowMaterial.SetColor("_Tint", new Color(.1f, 1, .64f, .26f));
        }
        var edge = new[] {
            new Vector2(-.18f,.5f), new Vector2(.18f,.5f), new Vector2(.18f,0),
            new Vector2(.52f,0), new Vector2(0,-.55f), new Vector2(-.52f,0), new Vector2(-.18f,0)
        };
        var vertices = new Vector3[edge.Length * 2];
        for (int i = 0; i < edge.Length; i++)
        {
            vertices[i] = new Vector3(edge[i].x, edge[i].y, -.06f);
            vertices[i + edge.Length] = new Vector3(edge[i].x, edge[i].y, .06f);
        }
        var triangles = new List<int> { 0, 1, 2, 0, 2, 6, 5, 3, 4, 9, 8, 7, 13, 9, 7, 11, 10, 12 };
        for (int i = 0; i < edge.Length; i++)
        {
            int next = (i + 1) % edge.Length;
            triangles.AddRange(new[] { i, i + 7, next + 7, i, next + 7, next });
        }
        fuelArrowMesh = new Mesh { name = "Extruded fuel direction arrow" };
        fuelArrowMesh.vertices = vertices; fuelArrowMesh.triangles = triangles.ToArray();
        fuelArrowMesh.RecalculateNormals(); fuelArrowMesh.RecalculateBounds();
    }

    void AddFuelArrowBlade(Transform parent, float yaw, float scale, Material material, string title)
    {
        var blade = new GameObject(title);
        blade.transform.SetParent(parent, false);
        blade.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        blade.transform.localScale = Vector3.one * scale;
        blade.AddComponent<MeshFilter>().sharedMesh = fuelArrowMesh;
        var renderer = blade.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void AdvancePickups()
    {
        if (fuelGlowMaterial != null)
            fuelGlowMaterial.SetColor("_Tint", new Color(.1f, 1, .64f, .2f + .08f * Mathf.Sin(Time.time * 4)));
        foreach (var p in pickups)
        {
            if (p.at < distance - 10) PlaceNextPickup(p);
            float gap = p.at - distance;
            bool visible = !p.taken && p.at < finishDistance - FuelFinishClearance && gap > -10 && gap < 420;
            p.root.gameObject.SetActive(visible);
            p.root.position = new Vector3(p.x, 0, gap);
            p.root.rotation = Quaternion.identity;
            if (!visible) continue;
            // Target about 18 pixels of arrow height on a 390-pixel phone viewport. The
            // previous fixed world size collapsed to 5-6 pixels and vanished behind riders.
            // Compensate for FOV as well as distance; only the guidance beacon changes scale.
            float perspectiveScale = gap * 2 * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * .046f / 1.05f;
            float markerScale = Mathf.Clamp(perspectiveScale, 1.8f, 24f);
            p.marker.localScale = Vector3.one * markerScale;
            // Its tip remains above rider helmets; the physical can stays grounded below it.
            p.marker.localPosition = new Vector3(0, 3f + markerScale * .55f + Mathf.Sin(Time.time * 4) * .08f, 0);
            p.marker.localRotation = Quaternion.Euler(0, Time.time * 85, 0);
            float sweptTravel = speed / 3.6f * Mathf.Min(Time.deltaTime, .05f);
            if (gap < 3 && gap > -3 - sweptTravel && Mathf.Abs(p.x - PlayerX) < 1.15f && jump < .4f)
            {
                p.taken = true;
                p.root.gameObject.SetActive(false);
                fuelStopsTaken++;
                fuel = Mathf.Min(1, fuel + FuelRefill);
                Feedback(fuelSound);
                Announce("FUEL +30%");
            }
        }
    }
}
