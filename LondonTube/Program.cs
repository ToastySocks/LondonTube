using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("You did not pass the N stops here. Please hit return and try again.");
                Console.ReadLine();
                return;
            }
            string startStation, nextStation, priorstation;
            int currentcntr;

            int numberofStops = Int32.Parse(args[0]);

            startStation = "East Ham";
            int stops = numberofStops;  //5

            // load the csv file into LondonList by calling the FromRouteCSV to assign to each class object...
            List<LondonTubeRoute> LondonList = File.ReadAllLines(@"C:\VSProjects\LondonTube\data\Londontubelines.csv")
                                           .Skip(1)
                                           .Select(v => LondonTubeRoute.FromRouteCSV(v))
                                            .ToList();
            // I also want to add the any reverse directions to make it easier to capture each possible stop from between stations.
            //this is excluding the overlap stations.
            LondonTubeRoute.AppendReverseRoute(ref LondonList);


            // LondonStopsLIst will be the list of stops based on the N stops and startStation....
            List<string> LondonStopsList = new List<string>();
            List<string> RoutesList = new List<string>();
            currentcntr = 0;


            //initializing the values....

            priorstation = startStation;
            nextStation = startStation;

            if (!FindNthStopStation(LondonList, stops, ref priorstation, ref nextStation, ref currentcntr, ref LondonStopsList, ref RoutesList))
                return;
            //I should have a complete list of LondonStops based on the N stops, but want to clean up any stops that are in the RoutesList that I have captured...
            //let's eliminate any stops by taking out any that is found in the RoutesList....
            bool bfound;
            for (int i = LondonStopsList.Count - 1; i > 0; i--)
            {
                bfound = RoutesList.Any(r => r == LondonStopsList[i]);
                if (bfound)
                {
                    //remove this route from the LondonStopsList...
                    LondonStopsList.Remove(LondonStopsList[i]);
                }
            }

            LondonStopsList.Sort();
            // Display on the console screen the results.... count will only be 0 if I changed code to accept parameters externally. Please feel free to change settings above...
            if (LondonStopsList.Count == 0)
                Console.WriteLine("Ooops, please check either your starting station or your entry for N stops. You may have entered something that doesn't exist.");
            else
            {
                Console.WriteLine("These are the " + LondonStopsList.Count + " station destinations based on given " + stops + " (N stops)  from " + startStation + ", in sorted order:");
                foreach (string s in LondonStopsList)
                {
                    Console.WriteLine(s);
                }
            }
            Console.ReadLine();

        }

        public static bool FindNthStopStation(List<LondonTubeRoute> LondonList, int stops, ref string priorstation, ref string nextStation, ref int currentcntr, ref List<string> LondonStopsList, ref List<string> RoutesList)
        {
            try
            {
                string searchstations;
                string prior;
                List<LondonTubeRoute> listofStops;
                if (currentcntr < stops)
                {
                    //assigning to local variables since I want to use this in my linq expression below. Cannot use ref variables inside Linq.... 
                    searchstations = nextStation;
                    prior = priorstation;

                    // searching thru my LondonList for any fromstation matching the station I'm searching for at this time but also want to make sure I skip any ToStation where I came from..
                    var query = from LondonTubeRoute in LondonList
                                where (LondonTubeRoute.fromStation == searchstations && LondonTubeRoute.toSTation != prior)
                                select LondonTubeRoute;

                    //store results of this linq query into listofStops list array....
                    listofStops = query.ToList();

                    //loop thru each of the listofstops....
                    foreach (LondonTubeRoute thisRoute in listofStops)
                    {

                        if (thisRoute.toSTation != priorstation)  //exclude the reverse route - don't want to go back where I cme from
                        {
                            currentcntr++;
                            nextStation = thisRoute.toSTation;
                            priorstation = thisRoute.fromStation;

                            //want to track all the possible station stops in this routeslist while I'm in here already. will be handy later on... if any of these stops are in the final station stop, then we know there's a shorter route and we want to exclude this from the final destination stop....
                            RoutesList.Add(priorstation);

                            //recursively function call . each time I call this function, I'm basically checking for the next station... going deeper. each time incrementing my currentcounter...
                            FindNthStopStation(LondonList, stops, ref priorstation, ref nextStation, ref currentcntr, ref LondonStopsList, ref RoutesList);
                        }
                        else
                            currentcntr--;

                    }
                    currentcntr--;
                    return true;
                }
                else
                {
                    //this is where I add the Nth station stop to the LondonStopsList
                    //also decrement my currentcntr, going 1 level from my recursion...
                    string answer = nextStation;
                    //only want to add if it's not in the list yet, else just skip loading into the LondStopsList...
                    bool bExists = LondonStopsList.Any(u => u == answer);

                    if (!bExists)
                        LondonStopsList.Add(nextStation);

                    currentcntr--;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());   //display error message 
                return false;
            }
        }


    }

    class LondonTubeRoute
    {

        public string tubeline;
        public string fromStation;
        public string toSTation;


        public static LondonTubeRoute FromRouteCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            LondonTubeRoute londonroutes = new LondonTubeRoute();
            londonroutes.tubeline = Convert.ToString(values[0]);
            londonroutes.fromStation = Convert.ToString(values[1]);
            londonroutes.toSTation = Convert.ToString(values[2]);
            return londonroutes;

        }
        // Want to include in my LondonList the reverse directions. I want to capture to ToStation --> FromStation but store it in the same master list so i can search for every possible station stop. DSH!
        public static bool AppendReverseRoute(ref List<LondonTubeRoute> LondonList)
        {
            try
            {
                List<LondonTubeRoute> ReverseRouteList = new List<LondonTubeRoute>();

                foreach (LondonTubeRoute thisRoute in LondonList)
                {
                    //search thru the LondonLIst where there are unique routes in the reverse direction. i.e. i don't want to append the Barking to East Ham since it already is being capture in District line.
                    // I want to append those stop such as Upney to Barking... sorry, can't think of a better way to do this at the moment...
                    if (!LondonList.Any(w => (w.fromStation == thisRoute.toSTation && w.toSTation == thisRoute.fromStation)))

                    {
                        LondonTubeRoute newReverseRoute = new LondonTubeRoute();
                        newReverseRoute.tubeline = thisRoute.tubeline;
                        newReverseRoute.fromStation = thisRoute.toSTation;
                        newReverseRoute.toSTation = thisRoute.fromStation;
                        ReverseRouteList.Add(newReverseRoute);
                    }
                }

                foreach (LondonTubeRoute thisRoute in ReverseRouteList)
                {
                    LondonList.Add(thisRoute);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }


    }
}
