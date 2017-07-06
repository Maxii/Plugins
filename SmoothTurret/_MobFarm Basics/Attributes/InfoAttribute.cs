using UnityEngine;
using System.Collections;

public class InfoAttribute : PropertyAttribute {
	public readonly int lines;

	public InfoAttribute ( int lineNum ) {
		lines = lineNum;
	}

}
