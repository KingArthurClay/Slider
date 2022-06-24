using UnityEngine;
using System;
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
			UnitManager.manager.friendlies.Add(this);
		} else {
			UnitManager.manager.enemies.Add(this);
		}
	}

	void UpdateOwningTile(MilitarySTile newTile) {
		//Do anything which needs to be done when we move across tiles

		transform.SetParent((Transform) newTile.GetComponent(typeof(Transform)));
		tile = newTile;
	}

	List<Unit> findAccessibleEnemies() {
		List<Unit> ret = new List<Unit>();

		List<Unit> unfound = new List<Unit>();
		unfound.AddRange(UnitManager.manager.enemies);

		int rowSize = SGrid.GetGridString().IndexOf('_');
		if (rowSize <= 0) {
			Debug.LogError("Invalid GridString " + SGrid.GetGridString() + " found when pathfinding.");
			return null; //Invalid array or other error
		}

		//Remove _ -> GridString is now a 1D array being access as a 2d array
		char[] c = {'_'};
		String world = SGrid.GetGridString().Trim(c);

		Breadcrumb b = new Breadcrumb(tile.islandId, null, world.IndexOf(Convert.ToChar(tile.islandId)));
		System.Collections.Generic.PriorityQueue<Breadcrumb, int> next = new System.Collections.Generic.PriorityQueue<Breadcrumb, int>();
		Dictionary<int, Breadcrumb> visited = new System.Collections.Generic.Dictionary<int, Breadcrumb>(); //This could just be a list of ints
		next.Enqueue(b, 0); //Start the queue

		while(next.Peek()) {
			b = next.Dequeue(); //Get the next tile in the queue

			int[] locs = {b.loc - 1, b.loc + 1, b.loc - rowSize, b.loc + rowSize }; //Location of the 4 tiles around the current one

			foreach (Unit e in unfound) {
				if (world[b.loc] == e.tile.islandId) { //If we've found an enemy's tile
					ret.Add(e); //Add the enemy to list of found enemies

					unfound.Remove(e); //Corresponding removal - no sense checking twice
				}
			}

			for (int i = 0; i < 4; i++) {
				if(world[locs[i]] != '#' && !visited.ContainsKey(world[locs[i]])) { //If this tile is not empty & unvisited
					//Add the new tile to the queue
					next.Enqueue(new Breadcrumb(world[locs[i]], b, locs[i], b.cost + 1), b.cost + 1);
				}
			}

			visited.Add(b.id, b);
		}

		return ret;
	}
	
	List<STile> findPathToEnemy(Unit e) {
		List<STile> ret = new List<STile>();

		//Get the size of a row - I'm too lazy to fix this whenever we change to a 4x4
		int rowSize = SGrid.GetGridString().IndexOf('_');
		if (rowSize <= 0) {
			Debug.LogError("Invalid GridString " + SGrid.GetGridString() + " found when pathfinding.");
			return null; //Invalid array or other error
		}

		//Remove _ -> GridString is now a 1D array being access as a 2d array
		char[] c = {'_'};
		String world = SGrid.GetGridString().Trim(c);

		Breadcrumb b = new Breadcrumb(tile.islandId, null, world.IndexOf(Convert.ToChar(tile.islandId)));
		PriorityQueue<Breadcrumb, int> next = new System.Collections.Generic.PriorityQueue<Breadcrumb, int>();
		Dictionary<int, Breadcrumb> visited = new System.Collections.Generic.Dictionary<int, Breadcrumb>(); //This could just be a list of ints
		next.Enqueue(b, 0); //Start the queue

		while(next.Peek()) {
			b = next.Dequeue(); //Get the next tile in the queue

			int[] locs = {b.loc - 1, b.loc + 1, b.loc - rowSize, b.loc + rowSize }; //Location of the 4 tiles around the current one

			for (int i = 0; i < 4; i++) {
				if(world[locs[i]] != '#' && !visited.ContainsKey(world[locs[i]])) { //If this tile is not empty & unvisited
					if (world[locs[i]] == e.tile.islandId) { //If we've found the enemy's tile
						ret.Add(e.tile); //Add the tile the enemy's standing on the list

						//reverse the trail of breadcrumbs we've left, add them to the list
						while (b != null) {
							ret.Add(SGrid.current.GetStile(b.id));
							b = b.parent;
						}

						//Return the list in the right order
						ret.Reverse();
						return ret;
					}

					//Add the new tile to the queue
					next.Enqueue(new Breadcrumb(world[locs[i]], b, locs[i], b.cost + 1), b.cost + 1);
				}
			}

			visited.Add(b.id, b);
		}

		return null;
	}

	private class Breadcrumb {
		public int id;
		public int loc;
		public Breadcrumb parent;
		public int cost;

		public Breadcrumb(int id, Breadcrumb parent, int loc = -1, int cost = 0) {
			this.id = id;
			this.cost = cost;
			this.parent = parent;
			this.loc = loc;
		}
	}

}