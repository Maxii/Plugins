using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GFGridRenderManager{
	private static List<GFGrid> gridList = new List<GFGrid>();
	//a getter (no setter) for the list of grids
	public static List<GFGrid> GridList{
		get{return gridList;}	
	}
	
	public static void AddGrid(GFGrid grid){
		if(!gridList.Contains(grid))
			gridList.Add(grid);
	}
	
	public static void RemoveGrid(GFGrid grid){
		if(gridList.Contains(grid))
			gridList.Remove(grid);
	}
}
