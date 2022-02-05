using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://en.wikipedia.org/wiki/Playing_cards_in_Unicode
namespace OFC_Maui.Models;

public class CardModel
{
    public int IntVal { get; set; }
    public string CardName { get {
            return OFC_Blazor.OFCevaluator.SolverHelper.IntToCard(IntVal);
        } set { 
            IntVal = OFC_Blazor.OFCevaluator.SolverHelper.CardToInt(value);
        } }
    public string ImagePath
    {
        get
        {
            if (IntVal == 1)
            {
                // https://commons.wikimedia.org/wiki/Category:SVG_playing_cards
                return $"cards/Card_back_06.svg";
            }
            else if (IntVal <= 0)
            {
                return $"cards/EmptyCard.svg";
            }
            var name = CardName;
            var ordinanName = name[0].ToString().ToUpper() switch
            {
                "A" => "1",
                "T" => "10",
                "J" => "11-JACK",
                "Q" => "12-QUEEN",
                "K" => "13-KING",
                _ => name[0].ToString()
            };
            var suitName = name[1].ToString().ToUpper() switch
            {
                "S" => "SPADE",
                "H" => "HEART",
                "C" => "CLUB",
                _ => "DIAMOND"
            };

            return $"cards/{suitName}-{ordinanName}.svg";
        }
    }
    public CardPlacement CardPlacement { get; set; }

    public CardModel(int intVal)
    {
        this.IntVal = intVal;
    }
}

public enum CardPlacement
{
    Front,
    Middle,
    Back,
    Draw,
    Discard,
    Available,
    Deck
}
