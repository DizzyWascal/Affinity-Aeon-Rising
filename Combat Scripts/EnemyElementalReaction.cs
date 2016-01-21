﻿using UnityEngine;
using System.Collections;

//Script Objective: Control and Apply elemental effects and reactions, normal length = 2

public class EnemyElementalReaction : MonoBehaviour 
{
	//Animator
	public Animator anim;
	
	private float chainReactionTime = 0.5f;

	public float chainDistance = 15f;

	[Range(0f, 1f)]
	public float explosiveDamagePercentage = 0.1f;
	
	//Access Stats
	private EnemyCombatCharacter combatStats;
	
	//Elemental Dust Particles
	public Transform[] elementalDust; 
	[HideInInspector]
	public Transform[] currentElementalDust = new Transform[4];
	
	//Elemental Reactants
	//0 - Earth, 1 - Fire, 2 - Lightning, 3 - Water
	[HideInInspector]
	public int[] reactantLengths = new int[4]; //There are currently 4 elements
	
	//Special Debuff Lengths
	//0 - Clay, 1 - Lava, 2 - Magnet, 3 - Explosion, 4 - Storm, 5 - Steam
	[HideInInspector]
	public int[] elementalEffect = new int[6];	//There are currently 6 elemental effects
	
	//In case of Environmental Areas maybe the length of certain things can be changed
	private int[] elementalEffectLength = new int[6];

	//VFX Effects
	public Transform[] statusParticles;

	private int maxPlayers = 0;
	private int maxEnemies = 0;
	
	
	// Use this for initialization
	void Awake () 
	{
		//Initialise Max Turns - this maybe altered by management through the SetReactionLengths function 
		elementalEffectLength[0] = 0;	//Jammed
		elementalEffectLength[1] = 0;	//Molten
		elementalEffectLength[2] = 0;	//Magnetised
		elementalEffectLength[3] = 0;	//Battered
		elementalEffectLength[4] = 0;	//Power Surge
		elementalEffectLength[5] = 0;	//Steam Cloud
		
		//Access stats
		combatStats = gameObject.GetComponent <EnemyCombatCharacter>();
	}	

	
	//This procedures is called when hit by an elemental attack
	public void ActivateElementalEffect(int _element)
	{
		if(CombatManager.players.Count > maxPlayers)
		{
			maxPlayers = CombatManager.players.Count;
		}
		if(CombatManager.enemies.Count > maxEnemies)
		{
			maxEnemies = CombatManager.enemies.Count;
		}

		//Correct the element integer to correct array standard
		_element --;
		
		//If the dust is already present then there is no need to instantiate
		if(!currentElementalDust[_element])
		{
			//Spawn Elemental Dust corresponding to element
			currentElementalDust[_element] = Instantiate (elementalDust[_element],
			                                              transform.position,
			                                              transform.rotation) as Transform; 
			//Parent the instantiated dust particle
			currentElementalDust[_element].SetParent (transform, true);
		}
		
		//Increase Elemental Dust Length
		reactantLengths[_element] = 3; 

		
		//Check for elemental Reactions
		CheckForElementalReactions ();

		//Check If more than 1 player and enemies and all enemies shattered, if so then apply Special Paradox
		if(!CombatManager.specialParadox)
		{
			if(ParadoxActivate ())
			{
				GameObject.FindGameObjectWithTag ("Combat Manager").SendMessage ("SpecialParadoxActivate", SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	//This procedures is called to check for any possibility of a reaction
	void CheckForElementalReactions()
	{
		//0 - Clay: Water(3) and Earth(0) - Inflicts Jammed
		if(currentElementalDust[3] && currentElementalDust[0])
		{
			//Inflict Jammed
			SetJammed ();

			//Lower Integrity
			AffectIntegrity();
		}
		
		//1 - Lava: Fire(1) and Earth(0) - Inflicts Molten
		if(currentElementalDust[1] && currentElementalDust[0])
		{
			//Inflict Molten
			SetMolten ();

			//Lower Integrity
			AffectIntegrity();
		}
		
		//2 - Magnet: Earth(0) and Lightning(2) - Inflicts Magnetised
		if(currentElementalDust[0] && currentElementalDust[2])
		{
			//Inflict Magnetised
			SetMagnetised ();

			//Lower Integrity
			AffectIntegrity();
		}
		
		//3 - Explosion: Fire(1) and Lightning(2) - Inflicts Battered
		if(currentElementalDust[1] && currentElementalDust[2])
		{
			//Inflict Battered
			SetBattered ();

			//Lower Integrity
			AffectIntegrity();
		}
		
		//4 - Storm: Water(3) and Lightning(2) - Inflicts Power Surge
		if(currentElementalDust[3] && currentElementalDust[2])
		{
			//Inflict Power Surge
			SetPowerSurge ();

			//Lower Integrity
			AffectIntegrity();
		}
		
		//5 - Steam: Fire(1) and Water(3) - Inflicts Steam Cloud
		if(currentElementalDust[1] && currentElementalDust[3])
		{
			//Inflict Steam Cloud
			SetSteamCloud ();

			//Lower Integrity
			AffectIntegrity();
		}
	}
	
	//Special Elemental Reaction Debuff Setters
	
	//Jammed, Created by a Clay Reaction - Greatly Decreases Speed
	public void SetJammed()
	{
		//If already inflicted
		if(elementalEffect[0] > 0)
		{
			//Add to the length
			elementalEffect[0] += elementalEffectLength[0]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[0] = elementalEffectLength[0]; 
			
			//Inflict Status Debuff
			//combatStats.stat.speed -= (int)((float)combatStats.stat.speedBase * 0.5f);
			
			//Spawn VFX here
			Instantiate (statusParticles[0], transform.position, transform.rotation);
		}
		
		//Eliminate Reactants (3 and 0)
		if(currentElementalDust[3])
		{
			Destroy (currentElementalDust[3].gameObject);
		}
		if(currentElementalDust[0])
		{
			Destroy (currentElementalDust[0].gameObject);
		}
		
		reactantLengths[3] = 0;
		reactantLengths[0] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (3) and (0)
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[3] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex -1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetJammed");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[3] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex +1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetJammed");
				}
			}
		}
	}
	
	//Molten, Created by a Lava Reaction - Greatly decreases defence, inflicts burning
	public void SetMolten()
	{
		//If already inflicted
		if(elementalEffect[1] > 0)
		{
			//Add to the length
			elementalEffect[1] += elementalEffectLength[1]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[1] = elementalEffectLength[1]; 
			
			//Inflict Status Debuff
			//combatStats.stat.defence -= (int)((float)combatStats.stat.defenceBase * 0.5f);
			
			//Inflict Burning
			//combatStats.SetBurning (elementalEffect[1]);
			
			//Spawn VFX here
			Instantiate (statusParticles[1], transform.position, transform.rotation);
		}
		
		//Eliminate Reactants (1 and 0)
		if(currentElementalDust[1])
		{
			Destroy (currentElementalDust[1].gameObject);
		}
		if(currentElementalDust[0])
		{
			Destroy (currentElementalDust[0].gameObject);
		}
		
		reactantLengths[1] = 0;
		reactantLengths[0] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (1) and (0)
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex -1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetMolten");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex +1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetMolten");
				}
			}
		}
	}
	
	//Magnetised, Created by a Magnetic Reaction - Greatly decreases agility
	public void SetMagnetised()
	{
		//If already inflicted
		if(elementalEffect[2] > 0)
		{
			//Add to the length
			elementalEffect[2] += elementalEffectLength[2]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[2] = elementalEffectLength[2]; 
			
			//Inflict Status Debuff
			//combatStats.stat.agility -= (int)((float)combatStats.stat.agilityBase * 0.5f);
			
			//Spawn VFX here
			Instantiate (statusParticles[2], transform.position, transform.rotation);
		}
		
		//Eliminate Reactants (2 and 0)
		if(currentElementalDust[2])
		{
			Destroy (currentElementalDust[2].gameObject);
		}
		if(currentElementalDust[0])
		{
			Destroy (currentElementalDust[0].gameObject);
		}
		
		reactantLengths[2] = 0;
		reactantLengths[0] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (1) and (0)
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[2] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex - 1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetMagnetised");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[2] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[0] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex + 1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetMagnetised");
				}
			}
		}
	}
	
	//Battered, Created by an Explosive Reaction - Causes AOE damage, reduces chance of attacking
	public void SetBattered()
	{
		//If already inflicted
		if(elementalEffect[3] > 0)
		{
			//Add to the length
			elementalEffect[3] += elementalEffectLength[3]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[3] = elementalEffectLength[3]; 
			
			//Spawn VFX here
			Instantiate (statusParticles[3], transform.position, transform.rotation);
		}

		//Set Explosive Damage here
		//int damage = (int)((float)combatStats.stat.healthBase * explosiveDamagePercentage);
		//combatStats.stat.health -= damage;
		//combatStats.ShowDamageText (damage.ToString (), Color.white, 1f);
		
		//Eliminate Reactants (1 and 2)
		if(currentElementalDust[1])
		{
			Destroy (currentElementalDust[1].gameObject);
		}
		if(currentElementalDust[2])
		{
			Destroy (currentElementalDust[2].gameObject);
		}
		
		reactantLengths[1] = 0;
		reactantLengths[2] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (1) and (0)
		//Special Note: Send AOE damage here
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[2] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex -1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetBattered");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[2] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex +1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetBattered");
				}
			}
		}
	}
	
	//Power Surge, Created by a Storm Reaction - Greatly Decreases AP
	public void SetPowerSurge()
	{
		//If already inflicted
		if(elementalEffect[4] > 0)
		{
			//Add to the length
			elementalEffect[4] += elementalEffectLength[4]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[4] = elementalEffectLength[4]; 
			
			//Inflict Status Debuff
			//combatStats.APCost ((int)((float)combatStats.stat.actionPointBase * 0.2f));
			
			//Spawn VFX here
			Instantiate (statusParticles[4], transform.position, transform.rotation);
		}
		
		//Eliminate Reactants (2 and 3)
		if(currentElementalDust[2])
		{
			Destroy (currentElementalDust[2].gameObject);
		}
		if(currentElementalDust[3])
		{
			Destroy (currentElementalDust[3].gameObject);
		}
		
		reactantLengths[2] = 0;
		reactantLengths[3] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (1) and (0)
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[2] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[3] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex -1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetPowerSurge");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[2] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[3] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex +1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetPowerSurge");
				}
			}
		}
	}
	
	//Steam Cloud, Created by Steam - Greatly decreases accuracy
	public void SetSteamCloud()
	{
		//If already inflicted
		if(elementalEffect[5] > 0)
		{
			//Add to the length
			elementalEffect[5] += elementalEffectLength[5]; 
		}
		else 
		{
			//If not yet inflicted
			
			//Reset the length
			elementalEffect[5] = elementalEffectLength[5]; 
			
			//Inflict Status Debuff
			//combatStats.stat.accuracy -= (int)((float)combatStats.stat.accuracyBase * 0.5f);
			
			//Spawn VFX here
			Instantiate (statusParticles[5], transform.position, transform.rotation);
		}
		
		//Eliminate Reactants (1 and 3)
		if(currentElementalDust[1])
		{
			Destroy (currentElementalDust[1].gameObject);
		}
		if(currentElementalDust[3])
		{
			Destroy (currentElementalDust[3].gameObject);
		}
		
		reactantLengths[1] = 0;
		reactantLengths[3] = 0;
		
		//Check for possibility of chain reaction
		
		//Obtain Current Index
		int currentIndex = combatStats.EnemyIndex ();
		
		//Communicate with neighbours and check if they have elements (1) and (0)
		//Floor Neighbour
		if(currentIndex > 0)
		{
			if(CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex - 1].elementalReaction.reactantLengths[3] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex -1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex - 1].elementalReaction.SetChainReaction ("SetSteamCloud");
				}
			}
		}
		
		//Ceiling Neighbour
		if(currentIndex < CombatManager.enemyStats.Count - 1)
		{
			if(CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[1] > 0 ||
			   CombatManager.enemyStats[currentIndex + 1].elementalReaction.reactantLengths[3] > 0)
			{
				//Get Distance
				float dist = Vector3.Distance (CombatManager.enemyStats[currentIndex +1].gameObject.transform.position,
				                               transform.position);
				if(dist < chainDistance)
				{
					//Chain Reaction - Clay to that neighbour
					CombatManager.enemyStats[currentIndex + 1].elementalReaction.SetChainReaction ("SetSteamCloud");
				}
			}
		}
	}
	
	//This function is called when the turn has ended
	public void UpdateElementalEffects()
	{
		//Update Jammed Status - Greatly Decreases Speed
		if(elementalEffect[0] > 0)
		{
			elementalEffect[0] --;
			
			if(elementalEffect[0] <= 0)
			{
				elementalEffect [0] = 0;
				
				//Restore Stat
				//combatStats.stat.speed += (int)((float)combatStats.stat.speedBase * 0.5f);
			}
		}
		
		//Update Molten - Greatly Decreases Defence, inflicts burning
		if(elementalEffect[1] > 0)
		{
			elementalEffect[1] --;
			
			if(elementalEffect[1] <= 0)
			{
				elementalEffect [1] = 0;
				
				//Restore Stat
				//combatStats.stat.defence += (int)((float)combatStats.stat.defenceBase * 0.5f);
			}
		}
		
		//Update Magnetised - Greatly Decreases Agility
		if(elementalEffect[2] > 0)
		{
			elementalEffect[2] --;
			
			if(elementalEffect[2] <= 0)
			{
				elementalEffect [2] = 0;
				
				//Restore Stat
				//combatStats.stat.agility += (int)((float)combatStats.stat.agilityBase * 0.5f);
			}
		}
		
		//Update Battered - AOE Damage and Reduces chance of attacking
		if(elementalEffect[3] > 0)
		{
			elementalEffect[3] --;
			
			if(elementalEffect[3] <= 0)
			{
				elementalEffect [3] = 0;
			}
		}
		
		//Update Power Surge - Greatly Decreases AP
		if(elementalEffect[4] > 0)
		{
			elementalEffect[4] --;

			//Inflict Status Debuff
			//combatStats.APCost ((int)((float)combatStats.stat.actionPointBase * 0.2f));
			
			if(elementalEffect[4] <= 0)
			{
				elementalEffect [4] = 0;
			}
		}
		
		//Update Steam - Greatly Decreases Accuracy
		if(elementalEffect[5] > 0)
		{
			elementalEffect[5] --;
			
			if(elementalEffect[5] <= 0)
			{
				elementalEffect [5] = 0;
				
				//Restore Stat
				//combatStats.stat.accuracy += (int)((float)combatStats.stat.accuracyBase * 0.5f);
			}
		}
	}

	public void SetChainReaction(string _status)
	{
		Invoke (_status, chainReactionTime);

		//Lower Integrity
		AffectIntegrity();

		//Check for elemental Reactions
		CheckForElementalReactions ();
		
		//Check If more than 1 player and enemies and all enemies shattered, if so then apply Special Paradox
		if(!CombatManager.specialParadox)
		{
			if(ParadoxActivate ())
			{
				GameObject.FindGameObjectWithTag ("Combat Manager").SendMessage ("SpecialParadoxActivate", SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	//This function is called to alter the elemental reaction effect lengths
	public void SetReactionLengths(int[] _lengths)
	{
		elementalEffectLength = _lengths;
	}

	//If more than 1 player and enemies and all enemies shattered
	private bool ParadoxActivate()
	{	
		bool allAffinityRevealed = true;

		for(int i = 0; i < CombatManager.enemies.Count; i++)
		{
			if(CombatManager.enemyStats[i].healthIntegrity > 0)
			{
				allAffinityRevealed = false;
			}
		}

		if(maxPlayers > 1 && maxEnemies > 1 && allAffinityRevealed)
		{
			return true;
		}

		return false;
	}

	void AffectIntegrity()
	{
		//In the Combat Character Script Decrement the Integrities
		if(combatStats.stat.shield > 0)
		{
			//If Shield is still active and the Shield is not revealed
			if(combatStats.shieldIntegrity > 0)
			{
				combatStats.shieldIntegrity--;			
			}
		}
		else
		{
			//If Shield is no longer active and health is not revealed
			if(combatStats.healthIntegrity > 0)
			{
				combatStats.healthIntegrity--;
				//print (combatStats.healthIntegrity);
			}
		}
	}
}