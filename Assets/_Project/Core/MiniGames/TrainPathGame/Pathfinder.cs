using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Pathfinder
{
    public class Node
    {
        public Station Station;
        public float Cost;
        public Node Previous;
        public bool Visited;
    }

    public static List<Station> FindShortestPath(Station start, Station end, 
        List<Station> allStations, List<TrainPathConnection> allPaths)
    {
        if (start == null || end == null || start == end)
            return new List<Station> { start };

        Dictionary<Station, Node> nodes = new Dictionary<Station, Node>();
        
        foreach (var station in allStations)
        {
            nodes[station] = new Node
            {
                Station = station,
                Cost = station == start ? 0f : float.MaxValue,
                Previous = null,
                Visited = false
            };
        }

        Node currentNode = nodes[start];
        
        while (currentNode != null && !currentNode.Visited)
        {
            currentNode.Visited = true;
            
            if (currentNode.Station == end)
            {
                return ReconstructPath(nodes[end]);
            }
            
            // Находим соседние станции
            List<TrainPathConnection> connections = allPaths.Where(p => 
                p.From == currentNode.Station || p.To == currentNode.Station).ToList();
            
            foreach (var path in connections)
            {
                Station neighbor = path.GetOtherStation(currentNode.Station);
                if (neighbor == null || !nodes.ContainsKey(neighbor))
                    continue;
                
                Node neighborNode = nodes[neighbor];
                if (neighborNode.Visited)
                    continue;
                
                // Стоимость = время пути + время ожидания на следующей станции
                float travelCost = path.TravelTime;
                float waitCost = neighbor == end ? 0f : neighbor.WaitTime;
                float newCost = currentNode.Cost + travelCost + waitCost;
                
                if (newCost < neighborNode.Cost)
                {
                    neighborNode.Cost = newCost;
                    neighborNode.Previous = currentNode;
                }
            }
            
            // Находим следующий узел с минимальной стоимостью
            currentNode = nodes.Values
                .Where(n => !n.Visited && n.Cost < float.MaxValue)
                .OrderBy(n => n.Cost)
                .FirstOrDefault();
        }
        
        return new List<Station>();
    }

    private static List<Station> ReconstructPath(Node endNode)
    {
        List<Station> path = new List<Station>();
        Node current = endNode;
        
        while (current != null)
        {
            path.Insert(0, current.Station);
            current = current.Previous;
        }
        
        return path;
    }
}
