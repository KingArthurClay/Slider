using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class MilitaryGrid : SGrid {

	static System.EventHandler<SGridAnimator.OnTileMoveArgs> handleUnits;

	public override void Init()
	{
		handleUnits  = (sender, e) => { UnitManager.manager.tileMovedUpdate(); };
	}

	private void OnEnable() {
		SGridAnimator.OnSTileMoveEnd += handleUnits;
	}

	private void OnDisable() {
		SGridAnimator.OnSTileMoveEnd -= handleUnits;
	}

}