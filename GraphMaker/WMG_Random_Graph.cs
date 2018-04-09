using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WMG_Random_Graph : MonoBehaviour {
	
	public GameObject graphManager;
	public Object nodePrefab;
	public Object linkPrefab;
	
	public int numNodes;
	public float minAngle;
	public float minAngleRange;
	public float maxAngleRange;
	public int minRandomNumberNeighbors;
	public int maxRandomNumberNeighbors;
	public float minRandomLinkLength;
	public float maxRandomLinkLength;
	public bool centerPropogate;
	public bool noLinkIntersection;
	public bool noNodeIntersection;
	public float noNodeIntersectionRadiusPadding;
	public int maxNeighborAttempts;
	public bool noLinkNodeIntersection;
	public float noLinkNodeIntersectionRadiusPadding;
	
	public bool createOnStart;
	public bool debugRandomGraph;
	private WMG_Graph_Manager theGraph;

	// Use this for initialization
	void Awake () {
		if (graphManager != null) {
			setManager(graphManager);
		}
		if (createOnStart) {
			GenerateGraph();
		}
	}
	
	public void setManager(GameObject managerObject) {
		graphManager = managerObject;
		theGraph = managerObject.GetComponent<WMG_Graph_Manager>();
	}
	
	public List<GameObject> GenerateGraph() {
		GameObject fromN = theGraph.CreateNode(nodePrefab, null);
		WMG_Node fromNode = fromN.GetComponent<WMG_Node>();
		return GenerateGraphFromNode(fromNode);
	}
	
	public List<GameObject> GenerateGraphFromNode(WMG_Node fromNode) {
		// Given a starting node, generate a graph of nodes around the starting node
		// Returns the list of nodes and links composing the resulting graph
		List<GameObject> returnResults = new List<GameObject>();
		returnResults.Add(fromNode.gameObject);
		
		// Initialize various variables used in the algorithm
		GameObject[] nodes = new GameObject[numNodes];
		bool[] nodesProcessed = new bool[numNodes];
		GameObject curObj = fromNode.gameObject;
		int procNodeNum = 0;
		int numNodesProcessed = 0;
		int numNodesStarting = theGraph.NodesParent.Count - 1;
		nodes[procNodeNum] = curObj;
		
		// Each while loop processes a node by attempting to create neighbors and links to neighbors from the node.
		// The loop ends when all nodes have been processed or when the number of nodes specified have been created.
		// A node is processed if all of its neighbors were successfully created or if not all neighbors were created, but maxNeighborAttempts was reached.
		// maxNeighborAttempts (a failed neighbor creation attempt) can get incremented for the following reasons:
		// 1. When a randomly generated angle falls between existing neighbors that is less than minAngle.
		// 2. If noLinkIntersection is true, a randomly generated angle and length would create a link that would cross an existing link in this manager's links parent.
		// 3. If noNodeIntersection is true, a randomly generated angle and length would create a node that that would circle interesect an existing node in this manager's nodes parent.
		// 3 cont. The same as above but noNodeIntersectionRadiusPadding > 0, performs the circle intersections check with the nodes' radii increased by the specified padding.
		// 4. If noLinkNodeIntersection is true, a randomly generated node would intersect with an existing link or a randomly generated link would intersect with an existing node.
		// 4 cont. The same as above but noLinkNodeIntersectionRadiusPadding > 0, performas the circle - line intersections with the node radius increased by the specified padding.
		while (theGraph.NodesParent.Count - numNodesStarting < numNodes) {
			
			WMG_Node procNode = nodes[procNodeNum].GetComponent<WMG_Node>();
			int numNeighbors = Random.Range(minRandomNumberNeighbors,maxRandomNumberNeighbors);
			if (debugRandomGraph) Debug.Log("Processesing Node: " + procNode.id + " with " + numNeighbors + " neighbors.");
			// Attempt to create a neighbor for the specified random number of neighbors
			for (int i = 0; i < numNeighbors; i++) {
				int curNeighborAttempt = 0;
				// For each neighbor, attempt to create the neighbor based on the maxNeighborAttempts
				while (curNeighborAttempt < maxNeighborAttempts) {
					// For this attempt, randomly generate an angle and length based on the specified parameters 
					float neighborAngle = Random.Range(minAngleRange,maxAngleRange);
					float neighborLength = Random.Range(minRandomLinkLength,maxRandomLinkLength);
					bool failedAttempt = false;
					
					if (debugRandomGraph) Debug.Log("Neighbor: " + i + " Attempt: " + curNeighborAttempt + " angle: " + Mathf.Round(neighborAngle));
					
					// Check to see that the randomly generated neighbor would not be too close to an existing neighbor (failure possibility #1)
					if (minAngle > 0) {
						for (int j = 0; j < procNode.numLinks; j++) {
							float angleDif = Mathf.Abs(procNode.linkAngles[j] - neighborAngle);
							if (angleDif > 180) angleDif = Mathf.Abs(angleDif - 360);
							if (angleDif < minAngle) {
								failedAttempt = true;
								break;
							}
						}
					}
					
					if (failedAttempt) {
						// Failed because random angle was smaller than the minAngle on either side of an existing neighbor
						if (debugRandomGraph) Debug.Log("Failed: Angle within minAngle of existing neighbor");
						curNeighborAttempt++;
						continue;
					}
					// Check if the randomly generated link intersects an existing link (failure possibility #2)
					if (noLinkIntersection) {
						float p1y = procNode.transform.localPosition.y + (neighborLength + procNode.radius) * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float p1x = procNode.transform.localPosition.x + (neighborLength + procNode.radius) * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						float p2y = procNode.transform.localPosition.y + procNode.radius * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float p2x = procNode.transform.localPosition.x + procNode.radius * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						foreach (GameObject child in theGraph.LinksParent) {
							WMG_Link childLink = child.GetComponent<WMG_Link>();
							if (childLink.id == -1) continue; // Dummy editor link
							WMG_Node childLinkFrom = childLink.fromNode.GetComponent<WMG_Node>();
							WMG_Node childLinkTo = childLink.toNode.GetComponent<WMG_Node>();
							float p3y = childLinkFrom.transform.localPosition.y;
							float p3x = childLinkFrom.transform.localPosition.x;
							float p4y = childLinkTo.transform.localPosition.y;
							float p4x = childLinkTo.transform.localPosition.x;
							if (PointInterArea(p1x, p1y, p2x, p2y, p3x, p3y) * PointInterArea(p1x, p1y, p2x, p2y, p4x, p4y) < 0 &&
								PointInterArea(p3x, p3y, p4x, p4y, p1x, p1y) * PointInterArea(p3x, p3y, p4x, p4y, p2x, p2y) < 0) { // Links intersect
								if (debugRandomGraph) Debug.Log("Failed: Link intersected with existing link: " + childLink.id);
								failedAttempt = true;
								break;
							}
						}
					}
					if (failedAttempt) {
						// Failed because random link intersected an existing link
						curNeighborAttempt++;
						continue;
					}
					// Check if the randomly generated node intersects an existing node (failure possibility #3)
					if (noNodeIntersection) {
						float p1y = procNode.transform.localPosition.y + (neighborLength) * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float p1x = procNode.transform.localPosition.x + (neighborLength) * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						foreach (GameObject child in theGraph.NodesParent) {
							WMG_Node aNode = child.GetComponent<WMG_Node>();
							if (aNode.id == -1) continue; // Dummy editor node
							// Circles intersect if (R0-R1)^2 <= (x0-x1)^2+(y0-y1)^2 <= (R0+R1)^2
							if (Mathf.Pow((p1x - child.transform.localPosition.x),2) + Mathf.Pow((p1y - child.transform.localPosition.y),2) <= Mathf.Pow(2*(procNode.radius + noNodeIntersectionRadiusPadding),2)) {
								if (debugRandomGraph) Debug.Log("Failed: Node intersected with existing node: " + aNode.id);
								failedAttempt = true;
								break;
							}
						}
					}
					if (failedAttempt) {
						// Failed because random node intersected an existing node
						curNeighborAttempt++;
						continue;
					}
					// Check if the randomly generated link intersects an existing node
					if (noLinkNodeIntersection) {
						float p1y = procNode.transform.localPosition.y + (neighborLength + procNode.radius) * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float p1x = procNode.transform.localPosition.x + (neighborLength + procNode.radius) * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						float p2y = procNode.transform.localPosition.y + procNode.radius * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float p2x = procNode.transform.localPosition.x + procNode.radius * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						foreach (GameObject child in theGraph.NodesParent) {
							WMG_Node aNode = child.GetComponent<WMG_Node>();
							if (procNode.id == aNode.id) continue; // Ignore the processesing node
							if (LineInterCircle(p1x, p1y, p2x, p2y, child.transform.localPosition.x, child.transform.localPosition.y, aNode.radius + noLinkNodeIntersectionRadiusPadding)) {
								if (debugRandomGraph) Debug.Log("Failed: Link intersected with existing node: " + aNode.id);
								failedAttempt = true;
								break;
							}
						}
					}
					if (failedAttempt) {
						// Failed because random link intersected an existing node
						curNeighborAttempt++;
						continue;
					}
					
					// Check if the randomly generated node intersects an existing link
					if (noLinkNodeIntersection) {
						float cy = procNode.transform.localPosition.y + (neighborLength + 2 * procNode.radius) * Mathf.Sin(Mathf.Deg2Rad*neighborAngle);
						float cx = procNode.transform.localPosition.x + (neighborLength + 2 * procNode.radius) * Mathf.Cos(Mathf.Deg2Rad*neighborAngle);
						foreach (GameObject child in theGraph.LinksParent) {
							WMG_Link childLink = child.GetComponent<WMG_Link>();
							if (childLink.id == -1) continue; // Dummy editor link
							WMG_Node childLinkFrom = childLink.fromNode.GetComponent<WMG_Node>();
							WMG_Node childLinkTo = childLink.toNode.GetComponent<WMG_Node>();
							float p1y = childLinkFrom.transform.localPosition.y;
							float p1x = childLinkFrom.transform.localPosition.x;
							float p2y = childLinkTo.transform.localPosition.y;
							float p2x = childLinkTo.transform.localPosition.x;
							if (LineInterCircle(p1x, p1y, p2x, p2y, cx, cy, procNode.radius + noLinkNodeIntersectionRadiusPadding)) {
								if (debugRandomGraph) Debug.Log("Failed: Node intersected with existing link: " + childLink.id);
								failedAttempt = true;
								break;
							}
						}
					}
					if (failedAttempt) {
						// Failed because random node intersected an existing link
						curNeighborAttempt++;
						continue;
					}
					
					// The attempt did not fail, so create the node and the link and break out of the while attempt < maxAttempts loop
					curObj = theGraph.CreateNode(nodePrefab, fromNode.transform.parent.gameObject);
					returnResults.Add(curObj);
					nodes[theGraph.NodesParent.Count - numNodesStarting - 1] = curObj;
					
//					curObj.transform.parent = fromNode.transform.parent;
					
					float dx = Mathf.Cos(Mathf.Deg2Rad*neighborAngle)*neighborLength;
					float dy = Mathf.Sin(Mathf.Deg2Rad*neighborAngle)*neighborLength;
					curObj.transform.localPosition = new Vector3(procNode.transform.localPosition.x + dx, procNode.transform.localPosition.y + dy, 0);
					
					returnResults.Add(theGraph.CreateLink(procNode, curObj, linkPrefab, null));
					break;
				}
				if (theGraph.NodesParent.Count - numNodesStarting == numNodes) break; // Max number nodes specified was reached, we are done generating the graph
			}
			// Set the node as processed and increment the processed node counter
			nodesProcessed[procNodeNum] = true;
			numNodesProcessed++;
			// Process the oldest node added as the next node to process
			if (centerPropogate) { 
				procNodeNum++;
			}
			// Pick a random node as the next node to process from the nodes that have been created from this algorithm
			else { 
				int numPossibleProcNodes = theGraph.NodesParent.Count - numNodesStarting - numNodesProcessed;
				if (numPossibleProcNodes > 0) {
					int[] possibleProcNodes = new int[numPossibleProcNodes];
					int j = 0;
					for (int i = 0; i < numNodes; i++) {
						if (!nodesProcessed[i] && i < theGraph.NodesParent.Count - numNodesStarting) {
							possibleProcNodes[j] = i;
							j++;
						}
					}
					procNodeNum = possibleProcNodes[Random.Range(0,j-1)];
				}
			}
			
			// This happens (algorithm ends prematurely) when maxNeighborAttempts was reached for the starting node or all the nodes created
			if (theGraph.NodesParent.Count - numNodesStarting == numNodesProcessed) { // Case where all nodes have been processed, but number nodes specfied were not created
				Debug.Log("WMG - Warning: Only generated " + (theGraph.NodesParent.Count - numNodesStarting - 1) + " nodes with the given parameters.");
				break;
			}
		}
		
		
		return returnResults;
	}
	
	float PointInterArea(float p1x, float p1y, float p2x, float p2y, float p3x, float p3y) {
		// Returns orientation of point 3 relative to line segment formed by points 1 and 2
		// If positive then clockwise, if negative then counterclockwise
		return (p2y - p1y) * (p3x - p2x) - (p2x - p1x) * (p3y - p2y);
	}
	
	bool LineInterCircle(float x0, float y0, float x1, float y1, float x2, float y2, float r) {
		// Returns true if a line segment intersect a circle
		// Line segment is (x0, y0) to (x1, y1). Circle is centered at (x2, y2) with radius r
		// Translate everything so that line segment start point to (0, 0)
		float a = x1-x0; // Line segment end point horizontal coordinate
		float b = y1-y0; // Line segment end point vertical coordinate
		float c = x2-x0; // Circle center horizontal coordinate
		float d = y2-y0; // Circle center vertical coordinate
		bool startInside = false;
		bool endInside = false;
		bool middleInside = false;
		if (r*r*(a*a + b*b) - (d*a - c*b)*(d*a - c*b) >= 0) {
		    // Collision is possible, discriminant is greater than or equal to 0
			if (c*c + d*d <= r*r) {
		      // Line segment start point is inside the circle, simply equation of circle start point (x0, y0) was translated by circle origin
		      startInside = true;
		    }
		    if ((a-c)*(a-c) + (b-d)*(b-d) <= r*r) {
		      // Line segment end point is inside the circle
		      endInside = true;
		    }
			if (!startInside && !endInside && c*a + d*b >= 0 && c*a + d*b <= a*a + b*b) {
		      // Middle section only
		      middleInside = true;
		    }
		}
		return startInside || endInside || middleInside;
	}
}
