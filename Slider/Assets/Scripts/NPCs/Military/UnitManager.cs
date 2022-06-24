using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Singleton manager for all the units on the stage. Holds list of player/enemy units
 */
public class UnitManager : MonoBehaviour {

	public static UnitManager manager { get; private set; }

	public List<Unit> enemies {get; private set;}
	public List<Unit> friendlies {get; private set;}

	void Awake() {
		if (manager) {
			Destroy(this);
		}
		manager = this;

		enemies = new List<Unit>();
		friendlies = new List<Unit>();
	}

	public void reset() {
		enemies = new List<Unit>();
		friendlies = new List<Unit>();
	}

	public void tileMovedUpdate() {
		//STFU Unity
	}

}