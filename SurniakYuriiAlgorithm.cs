using Robot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurniakYurii.RobotChallange {
    public class SurniakYuriiAlgorithm: IRobotAlgorithm {

        public int CountRound { get; set; } = 0;
        int robotCount = 10;


        public Dictionary<Position, int> _claimedStations;


        public SurniakYuriiAlgorithm() {
            Logger.OnLogRound += Logger_OnLogRound;

            _claimedStations = new Dictionary<Position, int>();
        }

        private void Logger_OnLogRound(object sender, LogRoundEventArgs e)
        {
            CountRound++;
        }

        string IRobotAlgorithm.Author => "Yurii Surniak";

        RobotCommand IRobotAlgorithm.DoStep(IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            Robot.Common.Robot movingRobot = robots[robotToMoveIndex];

            if (CountRound >= 25)
            {
                var nearbyStationAfter30 = FindNearestStationWithinRange(movingRobot.Position, map, robotToMoveIndex);

                if (nearbyStationAfter30 != null)
                {
                    if (ShouldCreateNewRobot(movingRobot, map, robots) && robotCount < 101)
                        return new CreateNewRobotCommand();
                    return new CollectEnergyCommand();
                }

                return new CollectEnergyCommand();
            }

            var nearbyStation = FindNearestStationWithinRange(movingRobot.Position, map, robotToMoveIndex);

            if (nearbyStation != null && movingRobot.Position == nearbyStation.Position)
            {
                if (ShouldCreateNewRobot(movingRobot, map, robots) && robotCount < 101)
                    return new CreateNewRobotCommand();
                return new CollectEnergyCommand();
            }

            if (_claimedStations.ContainsValue(robotToMoveIndex))
            {
                Position stationPos = _claimedStations
                    .First(x => x.Value == robotToMoveIndex)
                    .Key;

                return new MoveCommand()
                {
                    NewPosition = GetEnergyOptimizedStep(movingRobot.Position, stationPos, movingRobot)
                };
            }
            else
            {
                var unoccupiedStations = FindBestStation(movingRobot, map, robotToMoveIndex);

                if (unoccupiedStations == null)
                {
                    var station = FindStation(movingRobot, map);
                    int distance = DistanceHelper.ShebyshevDistance(movingRobot.Position, station.Position);

                    if (distance <= 3)
                    {
                        return new CollectEnergyCommand();
                    }
                    else
                    {
                        return new MoveCommand()
                        {
                            NewPosition = GetEnergyOptimizedStep(movingRobot.Position, station.Position, movingRobot)
                        };
                    }
                }
                else
                {
                    return new MoveCommand()
                    {
                        NewPosition = GetEnergyOptimizedStep(movingRobot.Position, unoccupiedStations.Position, movingRobot)
                    };
                }

            }
 
        }

        private EnergyStation FindNearestStationWithinRange(Position pos, Map map, int robotToMoveIndex)
        {
            EnergyStation nearestStation = null;
            int minDistance = int.MaxValue;

            foreach (var station in map.Stations)
            {
                int distance = DistanceHelper.ShebyshevDistance(pos, station.Position);

                if (distance > 3)
                    continue;

                if (_claimedStations.TryGetValue(station.Position, out int claimedBy) && claimedBy != robotToMoveIndex)
                    continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStation = station;
                }
            }

            return nearestStation;
        }

        private EnergyStation FindBestStation(Robot.Common.Robot movingRobot, Map map, int indexRobot)
        {
            EnergyStation nearestStation = null;
            int minDistance = int.MaxValue;

            foreach (var station in map.Stations)
            {
                if (_claimedStations.ContainsKey(station.Position))
                    continue;

                int distance = DistanceHelper.FindDistance(movingRobot.Position, station.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStation = station;
                }
            }

            if (nearestStation != null)
            {
                _claimedStations[nearestStation.Position] = indexRobot;
            }

            return nearestStation;
        }

        private EnergyStation FindStation(Robot.Common.Robot movingRobot, Map map)
        {
            EnergyStation nearestStation = null;
            int minDistance = int.MaxValue;

            foreach (var station in map.Stations)
            {
                int distance = DistanceHelper.FindDistance(movingRobot.Position, station.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStation = station;
                }
            }

            return nearestStation;
        }

        // Просто хід на 1 
        public Position GetNextStep(Position from, Position to)
        {
            int deltaX = 0;
            int deltaY = 0;

            if (to.X > from.X)
                deltaX = 1;
            else if (to.X < from.X)
                deltaX = -1;

            if (to.Y > from.Y)
                deltaY = 1;
            else if (to.Y < from.Y)
                deltaY = -1;

            return new Position(from.X + deltaX, from.Y + deltaY);
        }

        public Position GetEnergyOptimizedStep(Position from, Position to, Robot.Common.Robot robot)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (dx == 0 && dy == 0)
                return from;

            
            int distanceToTarget = dx * dx + dy * dy;

            if (distanceToTarget <= robot.Energy)
            {
                return new Position(to.X, to.Y);
            }

            for (int n = 2; n <= 1000; n++)
            {
                double energyPerStep = (double)robot.Energy / n;

                double requiredEnergyPerStep = (double)distanceToTarget / (n * n);
                
                if (requiredEnergyPerStep <= energyPerStep)
                {
                    double stepX = (double)dx / n;
                    double stepY = (double)dy / n;
                    
                    int newX = from.X + (int)Math.Round(stepX);
                    int newY = from.Y + (int)Math.Round(stepY);
                    
                    int actualDx = newX - from.X;
                    int actualDy = newY - from.Y;
                    int actualEnergyCost = actualDx * actualDx + actualDy * actualDy;
                    
                    if (actualEnergyCost <= robot.Energy && actualEnergyCost > 0)
                    {
                        return new Position(newX, newY);
                    }
                }
            }

            return GetNextStep(from, to);
        }

        private bool ShouldCreateNewRobot(Robot.Common.Robot robot, Map map, IList<Robot.Common.Robot> allRobots)
        {
            if (robot == null) return false;
            if (map == null) return false;

            if (robot.Energy <= 200)
                return false;

            robotCount++;
            return true;
        }

    }
}
