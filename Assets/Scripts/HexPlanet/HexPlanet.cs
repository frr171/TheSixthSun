﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The main HexPlanet script.
/// </summary>
public class HexPlanet : MonoBehaviour {
	/// <summary>The planet mesh built procedurally.</summary>
	Mesh mesh;

	/// <summary>Dictionary used to store graph in an edge-list format.</summary>
	public Dictionary<int, HashSet<int>> graph;

	/// <summary>Dictionary used to lookup the node number from a cartesian position</summary>
	public Dictionary<Vector3, int> vertex_to_node;

	/// <summary>Dictionary used to convert node ids to a position</summary>
	public Dictionary<int, Vector3> node_to_vertex;

	/// <summary>
	/// Gets the node that is neares to a given position.
	/// </summary>
	/// <returns>The nearest node.</returns>
	/// <param name="position">The position to be converted to a node number.</param>
	public int getNearestNode(Vector3 position) {
		int nearest_node = -1;
		float nearest_distance = 0.0f;
		foreach(KeyValuePair<Vector3, int> entry in vertex_to_node) {
			float distance = Vector3.Distance(position, entry.Key);
			if(nearest_node < 0 || distance < nearest_distance) {
				nearest_node = entry.Value;
				nearest_distance = distance;
			}
		}
		return nearest_node;
	}

	/// <summary>Converts a node index to a position. Should be used for placing units and buildings.</summary>
	public Vector3 getNodePosition(int node) {
		return node_to_vertex[node];
	}

	/// <summary> Returns the Orientation that an object stationed on a particular node should have. </summary>
	public Quaternion getNodeOrientation(int node) {
		return Quaternion.LookRotation(Vector3.Cross(getNodePosition(node).normalized, Vector3.forward), getNodePosition(node).normalized);
	}

	/// <summary>
	/// Gets the indices of the nodes that neigbor the current node index.
	/// </summary>
	/// <returns>An integer array of all of the neigboring node indices.</returns>
	/// <param name="node">The initial node index.</param>
	public int[] getNodeNeigbors(int node) {
		int[] neighbors = new int[graph[node].Count];
		graph[node].CopyTo(neighbors);
		return neighbors;
	}

	/// <summary>
	/// Returns all the nodes within a certain movement range of the given tile.
	/// </summary>
	/// <returns>A HashSet<int> of moveable nodes.</returns>
	/// <param name="startNode">The start node index.</param>
	/// <param name="range">The range that the given unit can move.</param>
	public HashSet<int> getMovementRange(int startNode, int range) {
		var moveableTiles = new HashSet<int>();

		var searchQueue = new Queue<int>();
		var ranges = new Dictionary<int, int>();

		searchQueue.Enqueue(startNode);
		ranges[startNode] = 0;

		while(searchQueue.Count > 0) {
			int currentNode = searchQueue.Dequeue();

			if(ranges[currentNode] <= range) {
				moveableTiles.Add(currentNode);

				foreach(int neighbor in getNodeNeigbors(currentNode)){
					if(!ranges.ContainsKey(neighbor)) {
						//TODO: Also confirm that this is a movable square

						ranges[neighbor] = ranges[currentNode] + 1;
						searchQueue.Enqueue(neighbor);
					}
				}
			}
		}

		moveableTiles.Remove(startNode);
		return moveableTiles;
	}

	// Use this for initialization
	void Start () {
	}

	/// <summary>
	/// When called, this function actually constructs the planet mesh, subdivides, and calculates the vertex-edge graph.
	/// </summary>
	public void CreatePlanet() {
		graph = new Dictionary<int, HashSet<int>>();
		vertex_to_node = new Dictionary<Vector3, int>();
		node_to_vertex = new Dictionary<int, Vector3>();

		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		
		List<Triangle> triangles = HexPlanetUtils.generateIcosohedron();

		for(int i = 0; i < GetComponent<PlanetBehavior>().planet.subdivisions; ++i) {
			Debug.Log("Subdividing...");
			//Subdivision phase
			List<Triangle> newTriangles = new List<Triangle>();
			foreach(Triangle tri in triangles) {
				Vector3 a = (tri.i/2 + tri.j/2).normalized;
				Vector3 b = (tri.j/2 + tri.k/2).normalized;
				Vector3 c = (tri.i/2 + tri.k/2).normalized;
				
				newTriangles.Add(new Triangle(tri.i, a, c));
				newTriangles.Add(new Triangle(tri.j, b, a));
				newTriangles.Add(new Triangle(tri.k, c, b));
				newTriangles.Add(new Triangle(a, b, c));
			}
			triangles = newTriangles;
		}
		
		
		mesh.vertices = Triangle.ToVertices(triangles);
		mesh.triangles = Triangle.ToIndices(triangles);
		
		//Generate UV Coordinates
		foreach(Triangle triangle in triangles) {
			triangle.i_uv = new Vector2(0.5f, -0.94999f);
			triangle.j_uv = new Vector2(0.8897f, -0.275f);
			triangle.k_uv = new Vector2(0.1103f, -0.275f);
		}
		mesh.uv = Triangle.ToUVS(triangles);
		
		//Calculate smoothed normals
		List<Vector3> normals = new List<Vector3>(mesh.vertices.Length);
		foreach(Vector3 vertex in mesh.vertices) normals.Add(vertex);
		mesh.normals = normals.ToArray();
		
		Debug.Log("Creating node-edge graph from mesh.");
		
		//Generate node-edge graph
		int id = 0;
		foreach(Vector3 vertex in mesh.vertices) {
			if(!vertex_to_node.ContainsKey(vertex)) {
				//Enable hashing in both directions
				vertex_to_node[vertex] = id;
				node_to_vertex[id] = vertex;
				++id;
			}
		}

		//Initialize empty edge lists
		foreach(int node in vertex_to_node.Values) {
			graph[node] = new HashSet<int>();
		}

		//Populate edge lists
		foreach(Triangle triangle in triangles) {
			//Add all possible edges to edge graph
			graph[vertex_to_node[triangle.i]].Add(vertex_to_node[triangle.j]);
			graph[vertex_to_node[triangle.i]].Add(vertex_to_node[triangle.k]);
			
			graph[vertex_to_node[triangle.j]].Add(vertex_to_node[triangle.i]);
			graph[vertex_to_node[triangle.j]].Add(vertex_to_node[triangle.k]);
			
			graph[vertex_to_node[triangle.k]].Add(vertex_to_node[triangle.i]);
			graph[vertex_to_node[triangle.k]].Add(vertex_to_node[triangle.j]);
		}
		
		mesh.RecalculateBounds();
	}
	
	// Update is called once per frame
	void Update () {

	}
}
