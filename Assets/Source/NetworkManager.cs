﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using SocketIO;
using System.Text.RegularExpressions;

public class NetworkManager : MonoBehaviour {
   
    public GameManager gameManager;
    public UIManager UIManager;
    public SocketIOComponent socket;

    
    public int debugcardID;
    public int debugplayerID;

    public Player myPlayer;

    public string msg;
	private string localServer = "ws://127.0.0.1:2000/socket.io/?EIO=4&transport=websocket";

	void Awake(){
		
		SocketIOComponent sic = socket.GetComponents<SocketIOComponent> ()[0];
		sic.url = localServer;
	}

    void Start()
    {
		
        StartCoroutine("ConnectToServer");

        socket.On("ASSIGN_ID", OnReceiveAssignID);
        socket.On("INIT_GAME", OnReceiveInitGame);
        socket.On("DRAW_CARD", OnReceiveDrawCard);
        socket.On("PLAY_CARD", OnReceivePlayCard);
        socket.On("INVALID_PLAY_CARD", OnReceiveInvalidPlayCard);
        socket.On("CHANGE_TURN", OnReceiveChangeTurn);
        socket.On("CHANGE_THEME", OnChangeTheme);
        socket.On("ASSIGN_CHARACTER", OnReceiveAssignCharacter);
        socket.On("DISCARD_CARD", OnReceiveDiscard);
        socket.On("UPDATE_SCORE", OnReceiveUpdateScore);
        socket.On("REFILL_HAND", OnReceiveRefillHand);
		socket.On("END_GAME", OnReceiveEndGame);

    }

    IEnumerator ConnectToServer()
    {
        yield return new WaitForSeconds(0.5f);
        
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["name"] = "UNITY";
        JSONObject jso = new JSONObject(data);
        socket.Emit("USER_CONNECT", jso);

        yield return new WaitForSeconds(1f);
    }

    public void SendPlayCard(int playerID, List<int> cardIDs)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["playerID"] = playerID.ToString();
		string st = "";
		for (int i = 0; i < cardIDs.Count; i++) {
			if (i == cardIDs.Count - 1)
				st += cardIDs [i];
			else
				st += cardIDs [i] + ",";
		}
        data["cardID"] = st;       

        JSONObject jso = new JSONObject(data);
        socket.Emit("PLAY_CARD", jso);
    }

    //Submit your play cards with your button


    public void SendEndTurn()
    {
        socket.Emit("END_TURN");
    }

    public void SendPower(string powerName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["Power_Name"] = powerName;
        data["player_ID"] = gameManager.myPlayer.idPlayer.ToString();
        JSONObject jso = new JSONObject(data);

        socket.Emit("USE_POWER", jso);
    }

    public void SendShame(int playerID, int opponentID, string theme, int cost, int ePoints)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data["Player_ID"] = playerID.ToString();
        data["Opponent_ID"] = opponentID.ToString();
        data["Theme"] = theme;
        data["Cost"] = cost.ToString();
        data["EarnedPoints"] = ePoints.ToString();

        JSONObject jso = new JSONObject(data);

        socket.Emit("SHAME", jso);
    }

    public void OnReceiveDiscard(SocketIOEvent e)
    {
        string playerID = e.data.GetField("playerID").ToString().Trim(new Char[] { '"' });
        string cardID = e.data.GetField("cardID").ToString();

        int pID;
        int.TryParse(playerID, out pID);
        int cID;
        int.TryParse(cardID, out cID);
        
        gameManager.discardCard(cID, pID);
    }

    public void OnReceiveRefillHand(SocketIOEvent e)
    {
        socket.Emit("ASK_FOR_CARDS");
    }

    public void OnChangeTheme(SocketIOEvent e)
    {
		print("Change theme received " + e.data);
        string a = e.data.GetField("theme").ToString();
        string themeName = a.Trim(new Char[] {'"'});
        gameManager.ChangeTheme(themeName);
    }

    public void OnReceiveUpdateScore(SocketIOEvent e)
    {
        string totalPoints = e.data.GetField("totalPoints").ToString();
        string totalInk = e.data.GetField("ink").ToString();
        string player = e.data.GetField("playerID").ToString();

        int playerID;
        int.TryParse(player, out playerID);
        int totPoints;
        int.TryParse(totalPoints.Trim(new Char[] { '"' }), out totPoints);
        int totInk;
        int.TryParse(totalInk.Trim(new Char[] { '"' }), out totInk);

        gameManager.UpdatePoints(playerID, totPoints);
        gameManager.UpdateInk(playerID, totInk);

        UIManager.CheckAvailableActions();
    }

    public void OnReceivePlayCard(SocketIOEvent e)
    {
        string cards = e.data.GetField("cards").ToString();
        string player = e.data.GetField("playerID").ToString();
        //string totalPoints = e.data.GetField("totalPoints").ToString();
        //string totalInk = e.data.GetField("ink").ToString();

        int playerID;
        int.TryParse(player, out playerID);
        /*int totPoints;
        int.TryParse(totalPoints.Trim(new Char[] {'"'}), out totPoints);
        int totInk;
        int.TryParse(totalInk.Trim(new Char[] { '"' }), out totInk);*/
        
        //gameManager.pointsDictionnary [player] += totPoints;

        //gameManager.UpdatePoints (playerID, totPoints);
        //gameManager.UpdateInk (playerID, totInk);

        gameManager.toPlay = new List<int>();
        
        var splitedCardsID = cards.Split(',');
        List<int> cardsToRemoveFromHand = new List<int>();
      
        foreach ( var c in splitedCardsID )
        {
            string cc = c.Trim(new Char[] { ' ', '"', ',' });
            int cardID;
            int.TryParse(cc, out cardID);
            gameManager.playCard(cardID);

            cardsToRemoveFromHand.Add(cardID);
        }
        
        if(gameManager.myPlayer.idPlayer == playerID)
        {
            gameManager.myPlayer.canPlay = false;
            UIManager.PlayCardsBt.interactable = false;
        } else
        {
            gameManager.ReOrderPlayerHands(playerID, cardsToRemoveFromHand);
        }

    }

    public void OnReceiveInvalidPlayCard(SocketIOEvent e)
    {
        string player = e.data.GetField("playerID").ToString();
        int playerID;
        int.TryParse(player, out playerID);

        print("INVALID PLAY CARD RECEIVED   " + e.data.GetField("cards").ToString());

        if(myPlayer.idPlayer == playerID)
        {
            string[] cards = e.data.GetField("cards").ToString().Split(',');
            gameManager.InvalidCardPlayed(cards);
        }

    }

    public void OnReceiveAssignID(SocketIOEvent e)
    {
        myPlayer.idPlayer = int.Parse(e.data["id"].ToString());
    }

    public void OnReceiveInitGame(SocketIOEvent e)
    {
        gameManager.initGame(myPlayer.idPlayer);
    }

    public void OnReceiveAssignCharacter(SocketIOEvent e)
    {

        List<string> charList = new List<string>();

        foreach (JSONObject character in e.data["chars"].list)
        {
            charList.Add(character.ToString().Trim(new Char[] { '"' }));
        }

        gameManager.AssignCharacters(charList);
    }

    public void OnReceiveChangeTurn(SocketIOEvent e)
    {
        int playerIdTurn = int.Parse(e.data["playerId"].ToString());

        if (myPlayer.idPlayer == playerIdTurn)
        {
            gameManager.startTurn();
        } else
        {
            gameManager.endTurn();
        }

        gameManager.cleanBoard();
		int turn = int.Parse(gameManager.Turns.text);
		turn -= 1;
		string text = turn.ToString (); 
		gameManager.Turns.text = text;
    }

    public void OnReceiveDrawCard(SocketIOEvent e)
    {
        gameManager.drawCard(int.Parse(e.data["id"].ToString()), int.Parse(e.data["playerId"].ToString()));
    }

	public void OnReceiveEndGame(SocketIOEvent e){

		Debug.Log ("OnReceiveEndGame " + e.data); 
		gameManager.CheckWinner (e.data ["id"].ToString());
	}


    string JsonToString(string target, string s)
    {
        string[] newString = Regex.Split(target, s);
        return newString[1];
    }
}
