using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EnergyPlusLib
{

    class Vertex
    {
       public Vertex(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z; 
        }
       double X{set;get;}
       double Y{set;get;}
       double Z{set;get;}
    }

    class Polygon
    {
        List<Vertex> VertexList {set; get;}
        Polygon(double Azimuth,
                double Tilt,
                Vertex StartingPoint,
                double Height,
                double Width);
        Polygon(List<Vertex> VertexList);
        List<Polygon> Triagulate();
        bool IsClosed();
        bool IsPlanar();
        float GetNormal();
        float GetArea();
        float GetPerimeter();
        void AddVertex(double X, double Y, double Z)
        {
            VertexList.Add(new Vertex(X, Y, Z));
        }
    }

    class Material
    {
        string Name { set; get; }
        string Roughness { set; get; }
        double Thickness { set; get; }
        double Conductivity { set; get; }
        double Density { set; get; }
        double SpecificHeat { set; get; }
        double ThermalAbsorptance { set; get; }
        double SolarAbsorptance { set; get; }
        double VisibleAbsorptance { set; get; }
    }

    class Construction
    {
        string Name { set; get; }
        List<Material> Materials;
    
    }

    class Surface 
    {
        enum Types { Wall = 0, Floor, Ceiling, Roof }
        string Name { set; get; }
        string Type { set; get; } //Wall, Floor,Ceiling,roof
        Construction Construction { set; get; }
        Zone Zone { set; get; }
        string BoundaryCondition { set; get; }
        string SunExposure { set; get; }
        string WindExposure { set; get; }
        double ViewFactor { set; get; }
        Polygon Polygon { set; get; }
        virtual List<Fenestration> GetFenestrations();
    }

    class Fenestration
    {
        string Name;
        enum SurfaceType { Window, Door, GlassDoor, TubularDaylightDome, TubularDaylightDiffuser}
        SurfaceType SurfaceType;
        Construction Construction;
        Surface Surface;
        string BoundaryCondition { set; get; }
        double ViewFactor { set; get; }
        string ShadingControlName { set; get; }
        string FrameandDividerName{ set; get; }
    }

    class Zone
    {
        string Name;
        double DirectionOfRelativeNorth;
        Vertex Origin;
        int Multiplier;
        double CeilingHeight; //optional
        double Volume;//optional
        string ZoneInsideConvectionAlgorithm; //default Simple
        string ZoneOutsideConvectionAlgorithm; //default Simple
        string PartofTotalFloorArea; //default Yes;
        virtual void AddSurface(Surface Surface);
        virtual void SetLoadsLighting();
        virtual void SetLoadsOccupancy();
        virtual void SetLoadPlugs();
        virtual void SetLoadsInfiltration();
        virtual void SetLoadsVentilation();
        virtual List<Surface> GetSurfaces();
    }

    class Building
    {

        //Fields Results
        DataTable _AnnualEndUseResults;
        DataTable _UnderHeatedHours;
        DataTable _UtilityCost;

        //Weatherfile.
        string _WeatherFile;

        //Results file.
        string _ResultsFile; // .sim file or .sql file. 

        //Simulation methods.
        virtual void RunSimulation(int startmonth, int startday, int endmonth,int endday);
        virtual void RunAnnualSimulation();
        virtual void LoadModelFile(string file);
        virtual void SaveModelFile(string file);
        virtual string GetErrorText();
        virtual bool CheckForErrors();

        //Construction methods these are low level methods that will be implemented in the 
        // building class.
        virtual Material AddMaterial();
        virtual Construction AddConstruction();
        virtual GlazingAssembly AddGlazingAssembly();
        virtual Zone AddZone();



        virtual Construction AddConstruction();




        //Utility methods. Maybe better placed into a singleton or web serivce. 
        virtual double NECB_GetFDWR(double HDD);
        virtual double GetHDD(String cityname);
        virtual double GetCDD(String cityname);

        //Query Methods

        //Common Charette methods. 
        virtual List<Building> ParametricWallInsulationAnalysis(double minConductance, double maxConductance, double increment) 
        { 
        // Create Ficticious Assembly based on A90.1/MNECB reference construction. 
           Construction construction = CreateFictiousConstruction();
           List<Surface> Walls = FindAllWalls();
           foreach( Surface wall in Walls)
            {
            wall.AssignConstruction(construction);
            }
        // Rename Simulation.
           this.RunSimulation();  
        // Iterate with decreased conductance.
        // store data is results set.
        }
        virtual List<Building> ParametricRoofInsulationAnalysis();
        virtual List<Building> ParametricFloorInsulationAnalysis();
        virtual List<Building> ParametericOccupantLoadAnalysis();
        virtual List<Building> ParametericLightingAnalysis();
        virtual List<Building> ParametericPlugLoadsAnalysis();
        virtual List<Building> ParametricGlazingConductanceAnalysis();
        virtual List<Building> ParametricGlazingTransmittanceAnalysis();
        virtual List<Building> ParametricInfiltrationAnalysis();
        virtual List<Building> ParametricVentilationAnalysis();
        virtual List<Building> ParametricThermalMassAnalysis();
        virtual List<Building> ParametericOrienationAnalysis();
        virtual List<Building> ParametericMassingAnalysis();
        virtual List<Building> ParametricPlantAnalysis(); //Boiler, Chiller, GSHP, SolarThermal, etc.
        virtual List<Building> ParametricSystemAnalysis();
        virtual List<Building> ParametricShadingAnalysis();
    }

    class DOEBuilding:Building
    {
    }

    class EPBuilding:Building
    {
        EPBuilding();
        EPBuilding(DOEBuilding doe_building){}

    }

    class gbXMLBuilding:Building
    {
    }
}
