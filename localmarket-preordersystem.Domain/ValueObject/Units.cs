using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Domain.ValueObject
{
    public enum Units
    {
        Piece = 1,
        Kilogram = 2,
        Gram = 3,
        Litre = 4,
        Millilitre = 5,
        Decagram = 6,
        Bundle = 7,
        Box = 8,
        Pack = 9,
        Bottle = 10,
        Jar = 11,
        Bunch = 12,
        Tray = 13
    }

    public static class UnitsExtensions
    {
        /// <summary>Darabolható (tört mennyiség nem értelmes): darab, csomag, doboz, üveg stb. A tömeg- és űrmértékek törtek lehetnek.</summary>
        public static bool IsCountable(this Units unit) => unit is not
            (Units.Kilogram or Units.Gram or Units.Decagram or Units.Litre or Units.Millilitre);

        /// <summary>
        /// A vevő által rendelhető legkisebb egység és lépték: kg és liter fél egységenként (0,5; 1; 1,5…),
        /// dkg tízesével, g és ml százasával, a darabos egységek egészenként.
        /// </summary>
        public static decimal OrderStep(this Units unit) => unit switch
        {
            Units.Kilogram or Units.Litre => 0.5m,
            Units.Decagram => 10m,
            Units.Gram or Units.Millilitre => 100m,
            _ => 1m
        };

        /// <summary>A mennyiség a lépték egész számú többszöröse-e (és pozitív).</summary>
        public static bool IsValidOrderQuantity(this Units unit, decimal quantity)
            => quantity > 0 && quantity % unit.OrderStep() == 0;
    }
}
