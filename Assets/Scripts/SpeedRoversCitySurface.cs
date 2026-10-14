using UnityEngine;
namespace SpeedRovers.Reborn
{
    public sealed partial class SpeedRoversGame
    {
        Material citySurfaceMaterial;
        Texture2D cityAsphalt, cityBuildings;
        RaceSpriteRegions cityBuildingRegions;
        bool DrawContinuousCitySurface(bool requireOpaque=false)
        {
            if(distance>5160 || (requireOpaque && distance+RoadProjection.ViewDistance>4800)) return false;
            if(citySurfaceMaterial==null)
            {
                Shader shader=Resources.Load<Shader>("Painted/CitySurface");
                if(shader==null || !shader.isSupported) return false;
                citySurfaceMaterial=new Material(shader);
                cityAsphalt=Resources.Load<Texture2D>("Painted/CityAsphalt");
            }
            float horizon=RoadProjection.ScreenY(distance,0);
            citySurfaceMaterial.SetFloat("_Distance",distance);
            citySurfaceMaterial.SetFloat("_Lane",lane);
            citySurfaceMaterial.SetFloat("_Horizon",horizon);
            citySurfaceMaterial.SetFloat("_Speed",speed);
            citySurfaceMaterial.SetColor("_Haze",sceneHaze);
            citySurfaceMaterial.SetColor("_Road",sceneRoad);
            citySurfaceMaterial.SetColor("_Marking",sceneMarking);
            Graphics.DrawTexture(new Rect(0,horizon,540,960-horizon),cityAsphalt!=null?cityAsphalt:asphalt,
                new Rect(0,0,1,1),0,0,0,0,Color.white,citySurfaceMaterial);
            return true;
        }
    }
}


