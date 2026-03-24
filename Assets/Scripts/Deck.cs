using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public GameObject dealer;
    public GameObject player;
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;

    public int[] values = new int[52];
    int cardIndex = 0;

    private bool gameOver = false;
    private bool dealerCardShown = false;

    private void Awake()
    {
        InitCardValues();
    }

    private void Start()
    {
        ShuffleCards();
        StartGame();
    }

    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            int rank = i % 13;

            if (rank == 0)
                values[i] = 11;      // Ace
            else if (rank >= 10)
                values[i] = 10;      // J, Q, K
            else
                values[i] = rank + 1; // 2..10
        }
    }

    private void ShuffleCards()
    {
        for (int i = 0; i < faces.Length; i++)
        {
            int randomIndex = Random.Range(i, faces.Length);

            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;

            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }

        cardIndex = 0;
    }

    void StartGame()
    {
        gameOver = false;
        dealerCardShown = false;

        hitButton.interactable = true;
        stickButton.interactable = true;
        playAgainButton.interactable = false;

        finalMessage.text = "";
        probMessage.text = "";

        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        if (playerPoints == 21 || dealerPoints == 21)
        {
            RevealDealerCard();

            if (playerPoints == 21 && dealerPoints == 21)
                EndGame("Empate. Ambos tienen Blackjack.");
            else if (playerPoints == 21)
                EndGame("Blackjack del jugador. Has ganado.");
            else
                EndGame("Blackjack del dealer. Has perdido.");
        }
        else
        {
            CalculateProbabilities();
        }
    }

    private void CalculateProbabilities()
    {
        if (gameOver)
            return;

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        int remainingCards = values.Length - cardIndex;

        if (remainingCards <= 0)
        {
            probMessage.text = "No quedan cartas.";
            return;
        }

        int dealerBetterCount = 0;
        int player17to21Count = 0;
        int playerBustCount = 0;

        for (int i = cardIndex; i < values.Length; i++)
        {
            int nextCard = values[i];

            int simulatedPlayer = SimulateScore(playerPoints, nextCard);
            int simulatedDealer = SimulateScore(dealerPoints, nextCard);

            if (simulatedDealer > playerPoints && simulatedDealer <= 21)
                dealerBetterCount++;

            if (simulatedPlayer >= 17 && simulatedPlayer <= 21)
                player17to21Count++;

            if (simulatedPlayer > 21)
                playerBustCount++;
        }

        float probDealerBetter = (float)dealerBetterCount / remainingCards * 100f;
        float prob17to21 = (float)player17to21Count / remainingCards * 100f;
        float probBust = (float)playerBustCount / remainingCards * 100f;

        probMessage.text =
            "Dealer > Player: " + probDealerBetter.ToString("F2") + "%\n" +
            "Player entre 17 y 21: " + prob17to21.ToString("F2") + "%\n" +
            "Player > 21: " + probBust.ToString("F2") + "%";
    }

    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        CalculateProbabilities();
    }

    public void Hit()
    {
        if (gameOver)
            return;

        RevealDealerCard();

        PushPlayer();

        int playerPoints = player.GetComponent<CardHand>().points;

        if (playerPoints > 21)
        {
            EndGame("El jugador se pasa de 21. Has perdido.");
            return;
        }

        if (playerPoints == 21)
        {
            Stand();
            return;
        }

        CalculateProbabilities();
    }

    public void Stand()
    {
        if (gameOver)
            return;

        RevealDealerCard();

        while (dealer.GetComponent<CardHand>().points <= 16)
        {
            PushDealer();
        }

        int dealerPoints = dealer.GetComponent<CardHand>().points;
        int playerPoints = player.GetComponent<CardHand>().points;

        if (dealerPoints > 21)
            EndGame("El dealer se pasa de 21. Has ganado.");
        else if (dealerPoints > playerPoints)
            EndGame("El dealer gana.");
        else if (dealerPoints < playerPoints)
            EndGame("El jugador gana.");
        else
            EndGame("Empate.");
    }

    public void PlayAgain()
    {
        hitButton.interactable = true;
        stickButton.interactable = true;
        playAgainButton.interactable = false;

        finalMessage.text = "";
        probMessage.text = "";

        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();

        cardIndex = 0;
        gameOver = false;
        dealerCardShown = false;

        ShuffleCards();
        StartGame();
    }

    private void EndGame(string message)
    {
        gameOver = true;
        finalMessage.text = message;
        hitButton.interactable = false;
        stickButton.interactable = false;
        playAgainButton.interactable = true;
    }

    private void RevealDealerCard()
    {
        if (!dealerCardShown)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            dealerCardShown = true;
        }
    }

    private int SimulateScore(int currentPoints, int nextCardValue)
    {
        int total = currentPoints + nextCardValue;

        if (nextCardValue == 11 && total > 21)
            total -= 10;

        return total;
    }
}