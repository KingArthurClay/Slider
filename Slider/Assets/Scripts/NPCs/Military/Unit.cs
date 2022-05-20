using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour {

	public static Side side {get; private set;}
	private MilitarySTile tile;
	private Transform transform;

	public enum Side {
		PLAYER,
		ENEMY
	}

	public Unit(Side sideySide) {
		side = sideySide;
	}

	void Start() {
		transform = (Transform) GetComponent(typeof(Transform));

		//TODO: Add unit to relevant UnitManager list
		if (side == Side.PLAYER) {
			UnitManager.manager.enemies.Add(this);
		} else {
			UnitManager.manager.enemies.Add(this);
		}
	}

	void UpdateOwningTile(MilitarySTile newTile) {
		//Do anything which needs to be done when we move across tiles

		transform.SetParent((Transform) newTile.GetComponent(typeof(Transform)));
		tile = newTile;
	}

	List<MilitarySTile> findPathToTile() {
		List<MilitarySTile> ret = new List<MilitarySTile>();
		


		return ret;
	}

	List<Unit> findAccessibleEnemies() {
		List<Unit> ret = new List<Unit>();
		


		return ret;
	}

}