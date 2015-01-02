namespace BdzServices.Models
{
    using System.Collections.Generic;

    public class Route
    {
        public Route(string departure, string arrival, int transitions, string duration, List<string> details)
        {
            this.ArrivalTime = arrival;
            this.DepartureTime = departure;
            this.Details = details;
            this.TripDuration = duration;
            this.Transitions = transitions;
        }

        public string DepartureTime { get; private set; }

        public string ArrivalTime { get; private set; }

        public int Transitions { get; private set; }

        public string TripDuration { get; private set; }

        public List<string> Details { get; private set; }


    }
}