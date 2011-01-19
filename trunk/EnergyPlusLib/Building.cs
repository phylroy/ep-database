using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EnergyPlusLib
{
    class Material{}

    class Construction{}

    class Surface {}

    class Zone
    {
        virtual void SetLoadsLighting();
        virtual void SetLoadsOccupancy();
        virtual void SetLoadPlugs();
        virtual void SetLoadsInfiltration();
        virtual void SetLoadsVentilation();
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
        virtual GlazingAssembly AddGlazingAssembly();
        virtual ExternalSurface AddExternalSurface();//Ground, Air, Adiabatic.
        virtual InternalSurface AddInternalSurface();
        virtual Schedule AddSchedule();
        virtual OccLoad AddOccLoad();
        virtual LightingLoad AddLightingLoad();
        virtual EquipmentLoad AddEquipementLoad();

        //Utility methods. Maybe better placed into a singleton or web serivce. 
        virtual double NECB_GetFDWR(double HDD)
        {
            // A linear interpolation of the formula is set by 
            // x-axis is HDD y-axis is FDWR% from 40/4000 and 20/7000.

        }
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
    }

    class gbXMLBuilding:Building
    {
    }
}
