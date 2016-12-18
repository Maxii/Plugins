#if !ASTAR_NO_GRID_GRAPH
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;

namespace Pathfinding {
	/** Grid Graph, supports layered worlds.
	 * The GridGraph is great in many ways, reliable, easily configured and updatable during runtime.
	 * But it lacks support for worlds which have multiple layers, such as a building with multiple floors.\n
	 * That's where this graph type comes in. It supports basically the same stuff as the grid graph, but also multiple layers.
	 * It uses a more memory, and is probably a bit slower.
	 * \note Does not support 8 connections per node, only 4.
	 *
	 * \ingroup graphs
	 * \shadowimage{layergridgraph_graph.png}
	 * \shadowimage{layergridgraph_inspector.png}
	 *
	 * \astarpro
	 */
	public class LayerGridGraph : GridGraph, IUpdatableGraph {
		//This function will be called when this graph is destroyed
		public override void OnDestroy () {
			base.OnDestroy();

			//Clean up a reference in a static variable which otherwise should point to this graph forever and stop the GC from collecting it
			RemoveGridGraphFromStatic();
		}

		void RemoveGridGraphFromStatic () {
			LevelGridNode.SetGridGraph(active.astarData.GetGraphIndex(this), null);
		}

		/** Number of layers.
		 * \warning Do not modify this variable
		 */
		[JsonMember]
		public int layerCount;

		/** If two layered nodes are too close, they will be merged */
		[JsonMember]
		public float mergeSpanRange = 0.5F;

		/** Nodes with a short distance to the node above it will be set unwalkable */
		[JsonMember]
		public float characterHeight = 0.4F;

		internal int lastScannedWidth;
		internal int lastScannedDepth;

		public new LevelGridNode[] nodes;

		public override bool uniformWidthDepthGrid {
			get {
				return false;
			}
		}

		public override int CountNodes () {
			if (nodes == null) return 0;

			int counter = 0;
			for (int i = 0; i < nodes.Length; i++) {
				if (nodes[i] != null) counter++;
			}
			return counter;
		}

		public override void GetNodes (GraphNodeDelegateCancelable del) {
			if (nodes == null) return;

			for (int i = 0; i < nodes.Length; i++) {
				if (nodes[i] != null && !del(nodes[i])) break;
			}
		}

		public new void UpdateArea (GraphUpdateObject o) {
			if (nodes == null || nodes.Length != width*depth*layerCount) {
				Debug.LogWarning("The Grid Graph is not scanned, cannot update area ");
				//Not scanned
				return;
			}

			//Copy the bounds
			Bounds b = o.bounds;

			//Matrix inverse
			//node.position = matrix.MultiplyPoint3x4 (new Vector3 (x+0.5F,0,z+0.5F));

			Vector3 min, max;
			GetBoundsMinMax(b, inverseMatrix, out min, out max);

			int minX = Mathf.RoundToInt(min.x-0.5F);
			int maxX = Mathf.RoundToInt(max.x-0.5F);

			int minZ = Mathf.RoundToInt(min.z-0.5F);
			int maxZ = Mathf.RoundToInt(max.z-0.5F);
			//We now have coordinates in local space (i.e 1 unit = 1 node)

			var originalRect = new IntRect(minX, minZ, maxX, maxZ);
			var affectRect = originalRect;

			var gridRect = new IntRect(0, 0, width-1, depth-1);

			var physicsRect = originalRect;

#if ASTARDEBUG
			Matrix4x4 debugMatrix = matrix;
			debugMatrix *= Matrix4x4.TRS(new Vector3(0.5f, 0, 0.5f), Quaternion.identity, Vector3.one);

			originalRect.DebugDraw(debugMatrix, Color.red);
#endif

			bool willChangeWalkability = o.updatePhysics || o.modifyWalkability;

			bool willChangeNodeInstances = (o is LayerGridGraphUpdate && ((LayerGridGraphUpdate)o).recalculateNodes);
			bool preserveExistingNodes = (o is LayerGridGraphUpdate ? ((LayerGridGraphUpdate)o).preserveExistingNodes : true);

			int erosion = o.updateErosion ? erodeIterations : 0;

			if (o.trackChangedNodes && willChangeNodeInstances) {
				Debug.LogError("Cannot track changed nodes when creating or deleting nodes.\nWill not update LayerGridGraph");
				return;
			}

			//Calculate the largest bounding box which might be affected

			if (o.updatePhysics && !o.modifyWalkability) {
				//Add the collision.diameter margin for physics calls
				if (collision.collisionCheck) {
					Vector3 margin = new Vector3(collision.diameter, 0, collision.diameter)*0.5F;

					min -= margin*1.02F;//0.02 safety margin, physics is rarely very accurate
					max += margin*1.02F;

					physicsRect = new IntRect(
						Mathf.RoundToInt(min.x-0.5F),
						Mathf.RoundToInt(min.z-0.5F),
						Mathf.RoundToInt(max.x-0.5F),
						Mathf.RoundToInt(max.z-0.5F)
						);

					affectRect = IntRect.Union(physicsRect, affectRect);
				}
			}

			if (willChangeWalkability || erosion > 0) {
				//Add affect radius for erosion. +1 for updating connectivity info at the border
				affectRect = affectRect.Expand(erosion + 1);
			}

			IntRect clampedRect = IntRect.Intersection(affectRect, gridRect);

			//Mark nodes that might be changed
			if (!willChangeNodeInstances) {
				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							o.WillUpdateNode(nodes[y*width*depth + z*width+x]);
						}
					}
				}
			}

			//Update Physics
			if (o.updatePhysics && !o.modifyWalkability) {
				collision.Initialize(matrix, nodeSize);

				clampedRect = IntRect.Intersection(physicsRect, gridRect);

				bool addedNodes = false;

				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						/** \todo FIX */
						addedNodes |= RecalculateCell(x, z, preserveExistingNodes);
					}
				}

				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

							CalculateConnections(nodes, node, x, z, y);
						}
					}
				}
			}

			//Apply GUO

			clampedRect = IntRect.Intersection(originalRect, gridRect);
			for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
				for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
					for (int y = 0; y < layerCount; y++) {
						int index = y*width*depth + z*width+x;

						var node = nodes[index];

						if (node == null) continue;

						if (willChangeWalkability) {
							node.Walkable = node.WalkableErosion;
							if (o.bounds.Contains((Vector3)node.position)) o.Apply(node);
							node.WalkableErosion = node.Walkable;
						} else {
							if (o.bounds.Contains((Vector3)node.position)) o.Apply(node);
						}
					}
				}
			}

#if ASTARDEBUG
			physicsRect.DebugDraw(debugMatrix, Color.blue);
			affectRect.DebugDraw(debugMatrix, Color.black);
#endif

			//Recalculate connections
			if (willChangeWalkability && erosion == 0) {
				clampedRect = IntRect.Intersection(affectRect, gridRect);
				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

							CalculateConnections(nodes, node, x, z, y);
						}
					}
				}
			} else if (willChangeWalkability && erosion > 0) {
				clampedRect = IntRect.Union(originalRect, physicsRect);

				IntRect erosionRect1 = clampedRect.Expand(erosion);
				IntRect erosionRect2 = erosionRect1.Expand(erosion);

				erosionRect1 = IntRect.Intersection(erosionRect1, gridRect);
				erosionRect2 = IntRect.Intersection(erosionRect2, gridRect);

#if ASTARDEBUG
				erosionRect1.DebugDraw(debugMatrix, Color.magenta);
				erosionRect2.DebugDraw(debugMatrix, Color.cyan);
#endif

				/*
				 * all nodes inside clampedRect might have had their walkability changed
				 * all nodes inside erosionRect1 might get affected by erosion from clampedRect and erosionRect2
				 * all nodes inside erosionRect2 (but outside erosionRect1) will be reset to previous walkability
				 * after calculation since their erosion might not be correctly calculated (nodes outside erosionRect2 would maybe have effect)
				 */

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

							bool tmp = node.Walkable;
							node.Walkable = node.WalkableErosion;

							if (!erosionRect1.Contains(x, z)) {
								//Save the border's walkabilty data in bit 16 (will be reset later)
								node.TmpWalkable = tmp;
							}
						}
					}
				}

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

#if ASTARDEBUG
							if (!node.Walkable)
								Debug.DrawRay((Vector3)node.position, Vector3.up*2, Color.red);
#endif
							CalculateConnections(nodes, node, x, z, y);
						}
					}
				}

				// Erode the walkable area
				ErodeWalkableArea(erosionRect2.xmin, erosionRect2.ymin, erosionRect2.xmax+1, erosionRect2.ymax+1);

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						if (erosionRect1.Contains(x, z)) continue;

						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

							// Restore temporarily stored data
							node.Walkable = node.TmpWalkable;
						}
					}
				}

				// Recalculate connections of all affected nodes
				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						for (int y = 0; y < layerCount; y++) {
							int index = y*width*depth + z*width+x;

							var node = nodes[index];

							if (node == null) continue;

							CalculateConnections(nodes, node, x, z, y);
						}
					}
				}
			}
		}

		public override void ScanInternal (OnScanStatus statusCallback) {
			if (nodeSize <= 0) {
				return;
			}

			GenerateMatrix();

			if (width > 1024 || depth > 1024) {
				Debug.LogError("One of the grid's sides is longer than 1024 nodes");
				return;
			}

			lastScannedWidth = width;
			lastScannedDepth = depth;

			SetUpOffsetsAndCosts();

			LevelGridNode.SetGridGraph(active.astarData.GetGraphIndex(this), this);

			maxClimb = Mathf.Clamp(maxClimb, 0, characterHeight);

			var linkedCells = new LinkedLevelCell[width*depth];

			collision = collision ?? new GraphCollision();
			collision.Initialize(matrix, nodeSize);

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					linkedCells[z*width+x] = new LinkedLevelCell();

					LinkedLevelCell llc = linkedCells[z*width+x];

					Vector3 pos = matrix.MultiplyPoint3x4(new Vector3(x+0.5F, 0, z+0.5F));


					RaycastHit[] hits = collision.CheckHeightAll(pos);

					for (int i = 0; i < hits.Length/2; i++) {
						RaycastHit tmp = hits[i];

						hits[i] = hits[hits.Length-1-i];
						hits[hits.Length-1-i] = tmp;
					}

					if (hits.Length > 0) {
						LinkedLevelNode lln = null;

						for (int i = 0; i < hits.Length; i++) {
							var tmp = new LinkedLevelNode();
							tmp.position = hits[i].point;

							if (lln != null) {
								/** \todo Use hit.distance instead */
								if (tmp.position.y - lln.position.y <= mergeSpanRange) {
									lln.position = tmp.position;
									lln.hit = hits[i];
									lln.walkable = collision.Check(tmp.position);
									continue;
								}
							}

							tmp.walkable = collision.Check(tmp.position);
							tmp.hit = hits[i];
							tmp.height = float.PositiveInfinity;

							if (llc.first == null) {
								llc.first = tmp;
								lln = tmp;
							} else {
								lln.next = tmp;

								lln.height = tmp.position.y - lln.position.y;
								lln = lln.next;
							}
						}
					} else {
						var lln = new LinkedLevelNode();
						lln.position = pos;
						lln.height = float.PositiveInfinity;
						lln.walkable = !collision.unwalkableWhenNoGround;
						llc.first = lln;
					}
				}
			}


			int spanCount = 0;
			layerCount = 0;
			// Count the total number of nodes in the graph
			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					LinkedLevelCell llc = linkedCells[z*width+x];

					LinkedLevelNode lln = llc.first;
					int cellCount = 0;
					// Loop through all nodes in this cell
					do {
						cellCount++;
						spanCount++;
						lln = lln.next;
					} while (lln != null);

					layerCount = cellCount > layerCount ? cellCount : layerCount;
				}
			}

			if (layerCount > LevelGridNode.MaxLayerCount) {
				Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (found "+layerCount+")");
				return;
			}

			// Create all nodes
			nodes = new LevelGridNode[width*depth*layerCount];
			for (int i = 0; i < nodes.Length; i++) {
				nodes[i] = new LevelGridNode(active);
				nodes[i].Penalty = initialPenalty;
			}

			int nodeIndex = 0;

			// Max slope in cosinus
			float cosAngle = Mathf.Cos(maxSlope*Mathf.Deg2Rad);

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					LinkedLevelCell llc = linkedCells[z*width+x];
					LinkedLevelNode lln = llc.first;

					llc.index = nodeIndex;

					int count = 0;
					int layerIndex = 0;
					do {
						var node = nodes[z*width+x + width*depth*layerIndex];
#if ASTAR_SET_LEVELGRIDNODE_HEIGHT
						node.height = lln.height;
#endif
						node.SetPosition((Int3)lln.position);
						node.Walkable = lln.walkable;

						// Adjust penalty based on the surface slope
						if (lln.hit.normal != Vector3.zero && (penaltyAngle || cosAngle < 1.0f)) {
							//Take the dot product to find out the cosinus of the angle it has (faster than Vector3.Angle)
							float angle = Vector3.Dot(lln.hit.normal.normalized, collision.up);

							// Add penalty based on normal
							if (penaltyAngle) {
								node.Penalty += (uint)Mathf.RoundToInt((1F-angle)*penaltyAngleFactor);
							}

							// Check if the slope is flat enough to stand on
							if (angle < cosAngle) {
								node.Walkable = false;
							}
						}

						node.NodeInGridIndex = z*width+x;

						if (lln.height < characterHeight) {
							node.Walkable = false;
						}

						node.WalkableErosion = node.Walkable;

						nodeIndex++;
						count++;
						lln = lln.next;
						layerIndex++;
					} while (lln != null);

					for (; layerIndex < layerCount; layerIndex++) {
						nodes[z*width+x + width*depth*layerIndex] = null;
					}

					llc.count = count;
				}
			}

			nodeIndex = 0;

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					for (int i = 0; i < layerCount; i++) {
						GraphNode node = nodes[z*width+x + width*depth*i];
						CalculateConnections(nodes, node, x, z, i);
					}
				}
			}

			uint graphIndex = (uint)active.astarData.GetGraphIndex(this);

			for (int i = 0; i < nodes.Length; i++) {
				var lgn = nodes[i];
				if (lgn == null) continue;

				UpdatePenalty(lgn);

				lgn.GraphIndex = graphIndex;

				// Set the node to be unwalkable if it hasn't got any connections
				if (!lgn.HasAnyGridConnections()) {
					lgn.Walkable = false;
					lgn.WalkableErosion = lgn.Walkable;
				}
			}

			ErodeWalkableArea();
		}

		/** Recalculates single cell.
		 *
		 * \param x X coordinate of the cell
		 * \param z Z coordinate of the cell
		 * \param preserveExistingNodes If true, nodes will be reused, this can be used to preserve e.g penalty when recalculating
		 *
		 * \returns If new layers or nodes were added. If so, you need to call
		 * AstarPath.active.DataUpdate() after this function to make sure pathfinding works correctly for them
		 * (when doing a scan, that function does not need to be called however).
		 *
		 * \note Connections are not recalculated for the nodes.
		 */
		public bool RecalculateCell (int x, int z, bool preserveExistingNodes) {
			var llc = new LinkedLevelCell();

			Vector3 pos = matrix.MultiplyPoint3x4(new Vector3(x+0.5F, 0, z+0.5F));


			RaycastHit[] hits = collision.CheckHeightAll(pos);

			for (int i = 0; i < hits.Length/2; i++) {
				RaycastHit tmp = hits[i];

				hits[i] = hits[hits.Length-1-i];
				hits[hits.Length-1-i] = tmp;
			}

			bool addedNodes = false;

			if (hits.Length > 0) {
				LinkedLevelNode lln = null;

				for (int i = 0; i < hits.Length; i++) {
					var tmp = new LinkedLevelNode();
					tmp.position = hits[i].point;

					if (lln != null) {
						/** \todo Use hit.distance instead */
						if (tmp.position.y - lln.position.y <= mergeSpanRange) {
							lln.position = tmp.position;
							lln.hit = hits[i];
							lln.walkable = collision.Check(tmp.position);
							continue;
						}
					}

					tmp.walkable = collision.Check(tmp.position);
					tmp.hit = hits[i];
					tmp.height = float.PositiveInfinity;

					if (llc.first == null) {
						llc.first = tmp;
						lln = tmp;
					} else {
						lln.next = tmp;

						lln.height = tmp.position.y - lln.position.y;
						lln = lln.next;
					}
				}
			} else {
				var lln = new LinkedLevelNode();
				lln.position = pos;
				lln.height = float.PositiveInfinity;
				lln.walkable = !collision.unwalkableWhenNoGround;
				llc.first = lln;
			}


			//=========

			uint graphIndex = (uint)active.astarData.GetGraphIndex(this);

			{
				//llc
				LinkedLevelNode lln = llc.first;

				int count = 0;
				int layerIndex = 0;
				do {
					if (layerIndex >= layerCount) {
						if (layerIndex+1 > LevelGridNode.MaxLayerCount) {
							Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (required "+(layerIndex+1)+")");
							return addedNodes;
						}

						AddLayers(1);
						addedNodes = true;
					}

					var node = nodes[z*width+x + width*depth*layerIndex];

					if (node == null || !preserveExistingNodes) {
						//Create a new node
						nodes[z*width+x + width*depth*layerIndex] = new LevelGridNode(active);
						node = nodes[z*width+x + width*depth*layerIndex];
						node.Penalty = initialPenalty;
						node.GraphIndex = graphIndex;
						addedNodes = true;
					}

					//node.connections = null;
#if ASTAR_SET_LEVELGRIDNODE_HEIGHT
					node.height = lln.height;
#endif
					node.SetPosition((Int3)lln.position);
					node.Walkable = lln.walkable;
					node.WalkableErosion = node.Walkable;

					//Adjust penalty based on the surface slope
					if (lln.hit.normal != Vector3.zero) {
						//Take the dot product to find out the cosinus of the angle it has (faster than Vector3.Angle)
						float angle = Vector3.Dot(lln.hit.normal.normalized, collision.up);

						//Add penalty based on normal
						if (penaltyAngle) {
							node.Penalty += (uint)Mathf.RoundToInt((1F-angle)*penaltyAngleFactor);
						}

						//Max slope in cosinus
						float cosAngle = Mathf.Cos(maxSlope*Mathf.Deg2Rad);

						//Check if the slope is flat enough to stand on
						if (angle < cosAngle) {
							node.Walkable = false;
						}
					}

					node.NodeInGridIndex = z*width+x;

					if (lln.height < characterHeight) {
						node.Walkable = false;
					}
					count++;
					lln = lln.next;
					layerIndex++;
				} while (lln != null);

				for (; layerIndex < layerCount; layerIndex++) {
					nodes[z*width+x + width*depth*layerIndex] = null;
				}

				llc.count = count;
			}

			return addedNodes;
		}

		/** Increases the capacity of the nodes array to hold more layers.
		 * After this function has been called and new nodes have been set up, the AstarPath.DataUpdate function must be called.
		 */
		public void AddLayers (int count) {
			int newLayerCount = layerCount + count;

			if (newLayerCount > LevelGridNode.MaxLayerCount) {
				Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (required "+newLayerCount+")");
				return;
			}

			LevelGridNode[] tmp = nodes;
			nodes = new LevelGridNode[width*depth*newLayerCount];
			for (int i = 0; i < tmp.Length; i++) nodes[i] = tmp[i];
			layerCount = newLayerCount;
		}

		/** Updates penalty for the node.
		 * This function sets penalty to zero (0) and then adjusts it if #penaltyPosition is set to true.
		 */
		public virtual void UpdatePenalty (LevelGridNode node) {
			node.Penalty = 0;//Mathf.RoundToInt (Random.value*100);
			node.Penalty = initialPenalty;

			if (penaltyPosition) {
				node.Penalty += (uint)Mathf.RoundToInt((node.position.y-penaltyPositionOffset)*penaltyPositionFactor);
			}
		}

		/** Erodes the walkable area. \see #erodeIterations */
		public override void ErodeWalkableArea (int xmin, int zmin, int xmax, int zmax) {
			// Clamp values to grid
			xmin = Mathf.Clamp(xmin, 0, Width);
			xmax = Mathf.Clamp(xmax, 0, Width);
			zmin = Mathf.Clamp(zmin, 0, Depth);
			zmax = Mathf.Clamp(zmax, 0, Depth);

			if (erosionUseTags) {
				Debug.LogError("Erosion Uses Tags is not supported for LayerGridGraphs yet");
			}

			for (int it = 0; it < erodeIterations; it++) {
				for (int l = 0; l < layerCount; l++) {
					for (int z = zmin; z < zmax; z++) {
						for (int x = xmin; x < xmax; x++) {
							var node = nodes[z*width+x + width*depth*l];
							if (node == null) continue;

							if (node.Walkable) {
								bool anyFalseConnections = false;

								// Check all four axis aligned connections
								for (int i = 0; i < 4; i++) {
									if (!node.GetConnection(i)) {
										anyFalseConnections = true;
										break;
									}
								}

								if (anyFalseConnections) {
									node.Walkable = false;
								}
							}
						}
					}
				}

				// Recalculate connections
				for (int l = 0; l < layerCount; l++) {
					for (int z = zmin; z < zmax; z++) {
						for (int x = xmin; x < xmax; x++) {
							LevelGridNode node = nodes[z*width+x + width*depth*l];
							if (node == null) continue;
							CalculateConnections(nodes, node, x, z, l);
						}
					}
				}
			}
		}

		/** Calculates the layered grid graph connections for a single node */
		public void CalculateConnections (GraphNode[] nodes, GraphNode node, int x, int z, int layerIndex) {
			if (node == null) return;

			var lgn = (LevelGridNode)node;
			lgn.ResetAllGridConnections();

			if (!node.Walkable) {
				return;
			}

			float height;
			if (layerIndex == layerCount-1 || nodes[lgn.NodeInGridIndex + width*depth*(layerIndex+1)] == null) {
				height = float.PositiveInfinity;
			} else {
				height = System.Math.Abs(lgn.position.y - nodes[lgn.NodeInGridIndex+width*depth*(layerIndex+1)].position.y)*Int3.PrecisionFactor;
			}

			for (int dir = 0; dir < 4; dir++) {
				int nx = x + neighbourXOffsets[dir];
				int nz = z + neighbourZOffsets[dir];

				//Check for out-of-bounds
				if (nx < 0 || nz < 0 || nx >= width || nz >= depth) {
					continue;
				}

				//Calculate new index
				int nIndex = nz*width+nx;
				int conn = LevelGridNode.NoConnection;

				for (int i = 0; i < layerCount; i++) {
					GraphNode other = nodes[nIndex + width*depth*i];
					if (other != null && other.Walkable) {
						float otherHeight;

						//Is there a node above this one
						if (i == layerCount-1 || nodes[nIndex+width*depth*(i+1)] == null) {
							otherHeight = float.PositiveInfinity;
						} else {
							otherHeight = System.Math.Abs(other.position.y - nodes[nIndex+width*depth*(i+1)].position.y)*Int3.PrecisionFactor;
						}

						float bottom = Mathf.Max(other.position.y*Int3.PrecisionFactor, lgn.position.y*Int3.PrecisionFactor);
						float top = Mathf.Min(other.position.y*Int3.PrecisionFactor+otherHeight, lgn.position.y*Int3.PrecisionFactor+height);

						float dist = top-bottom;

						if (dist >= characterHeight && Mathf.Abs(other.position.y-lgn.position.y)*Int3.PrecisionFactor <= maxClimb) {
							//Debug.DrawLine (lgn.position,other.position,new Color (0,1,0,0.5F));
							conn = i;
						}
					}
				}

				lgn.SetConnectionValue(dir, conn);
			}
		}


		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint) {
			if (nodes == null || depth*width*layerCount != nodes.Length) {
				//Debug.LogError ("NavGraph hasn't been generated yet");
				return new NNInfo();
			}

			var graphPosition = inverseMatrix.MultiplyPoint3x4(position);

			int x = Mathf.Clamp(Mathf.RoundToInt(graphPosition.x-0.5F), 0, width-1);
			int z = Mathf.Clamp(Mathf.RoundToInt(graphPosition.z-0.5F), 0, depth-1);

			var minNode = GetNearestNode(position, x, z, null);
			return new NNInfo(minNode);
		}

		private LevelGridNode GetNearestNode (Vector3 position, int x, int z, NNConstraint constraint) {
			int index = width*z+x;
			float minDist = float.PositiveInfinity;
			LevelGridNode minNode = null;

			for (int i = 0; i < layerCount; i++) {
				LevelGridNode node = nodes[index + width*depth*i];
				if (node != null) {
					float dist =  ((Vector3)node.position - position).sqrMagnitude;
					if (dist < minDist && (constraint == null || constraint.Suitable(node))) {
						minDist = dist;
						minNode = node;
					}
				}
			}
			return minNode;
		}

		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			if (nodes == null || depth*width*layerCount != nodes.Length || layerCount == 0) {
				return new NNInfo();
			}

			Vector3 globalPosition = position;

			position = inverseMatrix.MultiplyPoint3x4(position);

			int x = Mathf.Clamp(Mathf.RoundToInt(position.x-0.5F), 0, width-1);
			int z = Mathf.Clamp(Mathf.RoundToInt(position.z-0.5F), 0, depth-1);

			LevelGridNode minNode;
			float minDist = float.PositiveInfinity;
			int overlap = getNearestForceOverlap;

			minNode = GetNearestNode(globalPosition, x, z, constraint);
			if (minNode != null) {
				minDist = ((Vector3)minNode.position-globalPosition).sqrMagnitude;
			}

			if (minNode != null) {
				if (overlap == 0) return new NNInfo(minNode);
				overlap--;
			}


			float maxDist = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistance : float.PositiveInfinity;
			float maxDistSqr = maxDist*maxDist;

			for (int w = 1;; w++) {
				int nx;
				int nz = z+w;

				// Check if the nodes are within distance limit
				if (nodeSize*w > maxDist) {
					return new NNInfo(minNode);
				}

				for (nx = x-w; nx <= x+w; nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					LevelGridNode node = GetNearestNode(globalPosition, nx, nz, constraint);
					if (node != null) {
						float dist = ((Vector3)node.position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist && dist < maxDistSqr) { minDist = dist; minNode = node; }
					}
				}

				nz = z-w;

				for (nx = x-w; nx <= x+w; nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					LevelGridNode node = GetNearestNode(globalPosition, nx, nz, constraint);
					if (node != null) {
						float dist = ((Vector3)node.position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist && dist < maxDistSqr) { minDist = dist; minNode = node; }
					}
				}

				nx = x-w;

				for (nz = z-w+1; nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					LevelGridNode node = GetNearestNode(globalPosition, nx, nz, constraint);
					if (node != null) {
						float dist = ((Vector3)node.position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist && dist < maxDistSqr) { minDist = dist; minNode = node; }
					}
				}

				nx = x+w;

				for (nz = z-w+1; nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					LevelGridNode node = GetNearestNode(globalPosition, nx, nz, constraint);
					if (node != null) {
						float dist = ((Vector3)node.position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist && dist < maxDistSqr) { minDist = dist; minNode = node; }
					}
				}

				if (minNode != null) {
					if (overlap == 0) return new NNInfo(minNode);
					overlap--;
				}
			}
		}

		/** Utility method used by Linecast.
		 * Required since LevelGridNode does not inherit from GridNode.
		 * Lots of ugly casting but it was better than massive code duplication.
		 *
		 * Returns null if the node has no connection in that direction
		 */
		protected override GridNodeBase GetNeighbourAlongDirection (GridNodeBase node, int direction) {
			var levelGridNode = node as LevelGridNode;

			if (levelGridNode.GetConnection(direction)) {
				return nodes[levelGridNode.NodeInGridIndex+neighbourOffsets[direction] + width*depth*levelGridNode.GetConnectionValue(direction)];
			}
			return null;
		}

		/** Returns if \a node is connected to it's neighbour in the specified direction */
		public static bool CheckConnection (LevelGridNode node, int dir) {
			return node.GetConnection(dir);
		}

		public override void OnDrawGizmos (bool drawNodes) {
			if (!drawNodes) {
				return;
			}

			base.OnDrawGizmos(false);

			if (nodes == null) {
				return;
			}

			PathHandler debugData = AstarPath.active.debugPathData;

			for (int n = 0; n < nodes.Length; n++) {
				var node = nodes[n];

				if (node == null || !node.Walkable) continue;

				Gizmos.color = NodeColor(node, AstarPath.active.debugPathData);

				if (AstarPath.active.showSearchTree && AstarPath.active.debugPathData != null) {
					if (InSearchTree(node, AstarPath.active.debugPath)) {
						PathNode nodeR = debugData.GetPathNode(node);
						if (nodeR != null && nodeR.parent != null) {
							Gizmos.DrawLine((Vector3)node.position, (Vector3)nodeR.parent.node.position);
						}
					}
				} else {
					for (int i = 0; i < 4; i++) {
						int conn = node.GetConnectionValue(i);//(node.gridConnections >> i*4) & 0xF;
						if (conn != LevelGridNode.NoConnection) {
							int nIndex = node.NodeInGridIndex + neighbourOffsets[i] + width*depth*conn;

							if (nIndex < 0 || nIndex >= nodes.Length) {
								continue;
							}

							GraphNode other = nodes[nIndex];

							if (other == null) continue;

							Gizmos.DrawLine((Vector3)node.position, (Vector3)other.position);
						}
					}
				}
			}
		}

		public override void SerializeExtraInfo (GraphSerializationContext ctx) {
			if (nodes == null) {
				ctx.writer.Write(-1);
				return;
			}

			ctx.writer.Write(nodes.Length);

			for (int i = 0; i < nodes.Length; i++) {
				if (nodes[i] == null) {
					ctx.writer.Write(-1);
				} else {
					ctx.writer.Write(0);
					nodes[i].SerializeNode(ctx);
				}
			}
		}

		public override void DeserializeExtraInfo (GraphSerializationContext ctx) {
			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				nodes = null;
				return;
			}

			nodes = new LevelGridNode[count];
			for (int i = 0; i < nodes.Length; i++) {
				if (ctx.reader.ReadInt32() != -1) {
					nodes[i] = new LevelGridNode(active);
					nodes[i].DeserializeNode(ctx);
				} else {
					nodes[i] = null;
				}
			}
		}

		public override void PostDeserialization () {
#if ASTARDEBUG
			Debug.Log("Grid Graph - Post Deserialize");
#endif

			GenerateMatrix();

			lastScannedWidth = width;
			lastScannedDepth = depth;

			SetUpOffsetsAndCosts();

			if (nodes == null || nodes.Length == 0) return;

			LevelGridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), this);

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					for (int i = 0; i < layerCount; i++) {
						LevelGridNode node = nodes[z*width+x + width*depth*i];

						if (node == null) {
							continue;
						}

						node.NodeInGridIndex = z*width+x;
					}
				}
			}
		}
	}

	/** Internal class used by the LayerGridGraph */
	public class LinkedLevelCell {
		public int count;
		public int index;

		public LinkedLevelNode first;
	}

	/** Internal class used by the LayerGridGraph */
	public class LinkedLevelNode {
		public Vector3 position;
		public bool walkable;
		public RaycastHit hit;
		public float height;
		public LinkedLevelNode next;
	}

	/** Describes a single node for the LayeredGridGraph.
	 * Works almost the same as a grid node, except that it also stores to which layer the connections go to
	 */
	public class LevelGridNode : GridNodeBase {
		public LevelGridNode (AstarPath astar) : base(astar) {
		}

		private static LayerGridGraph[] _gridGraphs = new LayerGridGraph[0];
		public static LayerGridGraph GetGridGraph (uint graphIndex) { return _gridGraphs[(int)graphIndex]; }

		public static void SetGridGraph (int graphIndex, LayerGridGraph graph) {
			if (_gridGraphs.Length <= graphIndex) {
				var gg = new LayerGridGraph[graphIndex+1];
				for (int i = 0; i < _gridGraphs.Length; i++) gg[i] = _gridGraphs[i];
				_gridGraphs = gg;
			}

			_gridGraphs[graphIndex] = graph;
		}

#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
		protected ushort gridConnections;
#else
		protected uint gridConnections;
#endif

#if ASTAR_SET_LEVELGRIDNODE_HEIGHT
		public float height;
#endif

		protected static LayerGridGraph[] gridGraphs;

#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
		public const int NoConnection = 0xF;
		private const int ConnectionMask = 0xF;
		private const int ConnectionStride = 4;
#else
		public const int NoConnection = 0xFF;
		public const int ConnectionMask = 0xFF;
		private const int ConnectionStride = 8;
#endif
		public const int MaxLayerCount = ConnectionMask;

		/** Removes all grid connections from this node */
		public void ResetAllGridConnections () {
			unchecked {
#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
				gridConnections = (ushort)-1;
#else
				gridConnections = (uint)-1;
#endif
			}
		}

		/** Does this node have any grid connections */
		public bool HasAnyGridConnections () {
			unchecked {
#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
				return gridConnections != (ushort)-1;
#else
				return gridConnections != (uint)-1;
#endif
			}
		}

		public void SetPosition (Int3 position) {
			this.position = position;
		}

		public override void ClearConnections (bool alsoReverse) {
			if (alsoReverse) {
				LayerGridGraph graph = GetGridGraph(GraphIndex);
				int[] neighbourOffsets = graph.neighbourOffsets;
				LevelGridNode[] nodes = graph.nodes;

				for (int i = 0; i < 4; i++) {
					int conn = GetConnectionValue(i);
					if (conn != LevelGridNode.NoConnection) {
						LevelGridNode other = nodes[NodeInGridIndex+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn];
						if (other != null) {
							// Remove reverse connection
							other.SetConnectionValue((i + 2) % 4, NoConnection);
						}
					}
				}
			}

			ResetAllGridConnections();

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.ClearConnections(alsoReverse);
#endif
		}

		public override void GetConnections (GraphNodeDelegate del) {
			int index = NodeInGridIndex;

			LayerGridGraph graph = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = graph.neighbourOffsets;
			LevelGridNode[] nodes = graph.nodes;

			for (int i = 0; i < 4; i++) {
				int conn = GetConnectionValue(i);
				if (conn != LevelGridNode.NoConnection) {
					LevelGridNode other = nodes[index+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn];
					if (other != null) del(other);
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.GetConnections(del);
#endif
		}

		public override void FloodFill (Stack<GraphNode> stack, uint region) {
			int index = NodeInGridIndex;

			LayerGridGraph graph = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = graph.neighbourOffsets;
			LevelGridNode[] nodes = graph.nodes;

			for (int i = 0; i < 4; i++) {
				int conn = GetConnectionValue(i);
				if (conn != LevelGridNode.NoConnection) {
					LevelGridNode other = nodes[index+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn];
					if (other != null && other.Area != region) {
						other.Area = region;
						stack.Push(other);
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.FloodFill(stack, region);
#endif
		}

		/** Is there a grid connection in that direction */
		public bool GetConnection (int i) {
			return ((gridConnections >> i*ConnectionStride) & ConnectionMask) != NoConnection;
		}

		/** Set which layer a grid connection goes to.
		 * \param dir Direction for the connection.
		 * \param value The layer of the connected node or #NoConnection if there should be no connection in that direction.
		 */
		public void SetConnectionValue (int dir, int value) {
#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
			gridConnections = (ushort)(gridConnections & ~((ushort)(NoConnection << dir*ConnectionStride)) | (ushort)(value << dir*ConnectionStride));
#else
			gridConnections = gridConnections & ~((uint)(NoConnection << dir*ConnectionStride)) | (uint)(value << dir*ConnectionStride);
#endif
		}

		/** Which layer a grid connection goes to.
		 * \param dir Direction for the connection.
		 * \returns The layer of the connected node or #NoConnection if there is no connection in that direction.
		 */
		public int GetConnectionValue (int dir) {
			return (int)((gridConnections >> dir*ConnectionStride) & ConnectionMask);
		}

		public override bool GetPortal (GraphNode other, List<Vector3> left, List<Vector3> right, bool backwards) {
			if (backwards) return true;

			LayerGridGraph graph = GetGridGraph(GraphIndex);
			int[] neighbourOffsets = graph.neighbourOffsets;
			LevelGridNode[] nodes = graph.nodes;
			int index = NodeInGridIndex;

			for (int i = 0; i < 4; i++) {
				int conn = GetConnectionValue(i);
				if (conn != LevelGridNode.NoConnection) {
					if (other == nodes[index+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn]) {
						Vector3 middle = ((Vector3)(position + other.position))*0.5f;
						Vector3 cross = Vector3.Cross(graph.collision.up, (Vector3)(other.position-position));
						cross.Normalize();
						cross *= graph.nodeSize*0.5f;
						left.Add(middle - cross);
						right.Add(middle + cross);
						return true;
					}
				}
			}

			return false;
		}

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			handler.PushNode(pathNode);
			UpdateG(path, pathNode);

			LayerGridGraph graph = GetGridGraph(GraphIndex);
			int[] neighbourOffsets = graph.neighbourOffsets;
			LevelGridNode[] nodes = graph.nodes;
			int index = NodeInGridIndex;

			for (int i = 0; i < 4; i++) {
				int conn = GetConnectionValue(i);
				if (conn != LevelGridNode.NoConnection) {
					LevelGridNode other = nodes[index+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn];
					PathNode otherPN = handler.GetPathNode(other);

					if (otherPN != null && otherPN.parent == pathNode && otherPN.pathID == handler.PathID) {
						other.UpdateRecursiveG(path, otherPN, handler);
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.UpdateRecursiveG(path, pathNode, handler);
#endif
		}

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			LayerGridGraph graph = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = graph.neighbourOffsets;
			uint[] neighbourCosts = graph.neighbourCosts;
			LevelGridNode[] nodes = graph.nodes;

			int index = NodeInGridIndex;

			for (int i = 0; i < 4; i++) {
				int conn = GetConnectionValue(i);
				if (conn != LevelGridNode.NoConnection) {
					GraphNode other = nodes[index+neighbourOffsets[i] + graph.lastScannedWidth*graph.lastScannedDepth*conn];

					if (!path.CanTraverse(other)) {
						continue;
					}

					PathNode otherPN = handler.GetPathNode(other);

					if (otherPN.pathID != handler.PathID) {
						otherPN.parent = pathNode;
						otherPN.pathID = handler.PathID;

						otherPN.cost = neighbourCosts[i];

						otherPN.H = path.CalculateHScore(other);
						other.UpdateG(path, otherPN);

						handler.PushNode(otherPN);
					} else {
						//If not we can test if the path from the current node to this one is a better one then the one already used
						uint tmpCost = neighbourCosts[i];

#if ASTAR_NO_TRAVERSAL_COST
						if (pathNode.G + tmpCost < otherPN.G)
#else
						if (pathNode.G + tmpCost + path.GetTraversalCost(other) < otherPN.G)
#endif
						{
							otherPN.cost = tmpCost;

							otherPN.parent = pathNode;

							other.UpdateRecursiveG(path, otherPN, handler);
						}
						//Or if the path from this node ("other") to the current ("current") is better
#if ASTAR_NO_TRAVERSAL_COST
						else if (otherPN.G+tmpCost < pathNode.G)
#else
						else if (otherPN.G+tmpCost+path.GetTraversalCost(this) < pathNode.G)
#endif
						{
							pathNode.parent = otherPN;
							pathNode.cost = tmpCost;

							UpdateRecursiveG(path, pathNode, handler);
						}
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.Open(path, pathNode, handler);
#endif
		}

		public override void SerializeNode (GraphSerializationContext ctx) {
			base.SerializeNode(ctx);
			ctx.SerializeInt3(position);
			ctx.writer.Write(gridFlags);
			ctx.writer.Write(gridConnections);
		}

		public override void DeserializeNode (GraphSerializationContext ctx) {
			base.DeserializeNode(ctx);
			position = ctx.DeserializeInt3();
			gridFlags = ctx.reader.ReadUInt16();
#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
			gridConnections = ctx.reader.ReadUInt16();
#else
			gridConnections = ctx.reader.ReadUInt32();
#endif
		}
	}

	/** GraphUpdateObject with more settings for the LayerGridGraph.
	 * \see Pathfinding.GraphUpdateObject
	 * \see Pathfinding.LayerGridGraph
	 */
	public class LayerGridGraphUpdate : GraphUpdateObject {
		/** Recalculate nodes in the graph. Nodes might be created, moved or destroyed depending on how the world has changed. */
		public bool recalculateNodes;

		/** If true, nodes will be reused. This can be used to preserve e.g penalty values when recalculating */
		public bool preserveExistingNodes = true;
	}
}
#endif
