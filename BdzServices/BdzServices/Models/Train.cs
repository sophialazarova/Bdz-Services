namespace BdzServices.Models
{
    using System.Linq;

    public class Train
    {
        public Train(string part1, string part2, string part3)
        {
            setTrainValue(part1);
            setTrainValue(part2);
            setTrainValue(part3);
        }

        public string Time { get;private set; }
        public string Town { get;private set; }
        public string TrainNumber { get;private set; }

        private void setTrainValue(string input)
        {
            if (input.IndexOf(":") >= 0)
            {
                this.Time = input;
            }
            else if (input.Any(c => char.IsDigit(c)))
            {
                this.TrainNumber = input;
            }
            else
            {
                this.Town = input;
            }
        }

    }
}