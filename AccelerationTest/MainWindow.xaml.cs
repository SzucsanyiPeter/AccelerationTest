using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AccelerationTest
{
    /**********************************************************
     Szenzor és aktuátor hálózatok kurzusra készített projektmunka
        Készítette:
        Szucsányu Erik Péter
        Szimkó Dániel
    Megpróbáltuk a lehető legtöbb tudást amit a félév folyamán kaptunk belevinni a projekt elkészítésébe.
    Figyeltünk a terjedelemre, szenzorok és aktuátorok terén a gyorsaságra, egyszerűségre.
    Projektünk típusából kifolyólag szükségtelennek és valószerűtlennek tartottuk egy szerver létrehozását, ezért maradt ez ki.
    Továbbá a projektben nincsenek kiemelten, explicit módon feltűntetve az egyes szenzorok illetve aktuátorok, a fejlesztés során az egyes vizsgálatok (if) tekintettül szenzoroknak,
    míg az utasítások kiadását (ikon megjelenítés, értékváltoztatás) tekintettük aktuátoroknak.

    Feladatok megosztása:

    Szucsányi Erik Péter:

        Kezelőfelület elkészítése, fejlesztése
        Sebesség változás-, és a hozzátartozó szenzorok és aktuátorok szimulálása
        Sebességmérő óra létrehozása, működtetése
        Akkumulátor kapacitás kezelése
        ABS szimulálása
        Irányjelzés
        Holttér figyelő rendszer
        Sávváltás szimulálása
        Kormánymozdilat működtetése és kanyarkövető fényszórók visszajelzése

    Szimkó Dániel:
        Tecnhnika háttér/segítség nyújtása a fejlesztői környezettel, számításokkal kapcsplatban
        Felhasználói felület finomítása, fejlesztése
        Hőmérsékle szimulálása
        Klimatizálás működtetése
        Táblafelismerő rendszer - sebességkorlátok létrehozása
        Szimuláció sebességének változtatása

    **********************************************************/
    public partial class MainWindow : Window
    {
        public static double Speed;                     // Aktuálsi sebesség
        public static int k;                            // Sebesség csúszkáról olvasott érték
        public static double A = 0;                     // Gyorsulás
        public static double SpeedToReach;              // Beállított cél sebesség
        public static int MaxSpeed = 50;                // Sebességkorlát
        public const double MaxAcceleration = 3.6;      // Maximális gyosulás
        public const double MaxDeceleration = 9;        // Maximális lassulás

        public static bool SpeedChange = true;          // Sebesség változásának detektálása, kapcsolóként működik
        public static int millisecs = 1000;             // Várakozási idő, osztva lesz a rate-el
        public static int rate = 30;                    // Mérés gyakorisága
        //public static int absOn = 0;
        public static double maxDec = (MaxDeceleration * MaxAcceleration) / rate; // Maximális lassulás
        public static double Dec = 0;                   // Aktuális lassulás
        public static double hanyados;                  // Gyorsulás számítás része
        public static double yChange;                   // Autó erőhatások visszajelzése

        public static double Battery = 100;             // Akkumulátor töltöttségii szintje
        public const int MaxRange = 300;                // Maximális megtehető távolság
        public static int Range = MaxRange;             // Hátralávő megtehető távolság
        public static int SpeedUp = 50;                 // Felgyorsítás

        public static double OutsideTemp = 20;          // Kinti hőmérséklet
        public static double InsideTemp = 20;           // Autóban lévő hőmérséklet
        public static double ClimateTarget = 20;        // Klíma beállított értéke
        public const double TempStep = 0.3;             // Hőmérséklet ingadozás

        public static int myLine = 3;                   // Aktuális sáv
        public static int myLinePrev = 3;               // Előző sáv
        public static int lines = 3;                    // Sávok
        public static double myCarPosition = 0;         // Autó elhelyezkedése, sávváltás miatt, X tengelyen pixelben megadva
        public static double TargetPosition = 0;        // Következő pozíció
        public static int[] lines_cnt = new int[] { 0, 0, 0 };  // Sávok várakoztatása
        public static bool[] lines_b = new bool[] { false, false, false };  // Adott sáv foglaltsága

        public static int index = 0;                    // Irányjelzés iránya: -1 -> bal, 1 -y jobb, 0 -> kikapcsolva
        public static int IndexCounter = 0;             // 3 irányjelző felvillanás
        public static bool IsLineChangePossible;        // Holttérfigyelő
        public static int SteeringAngle = 0;            // Kormány szögállás

        public static Random random = new Random();     // Számgeneráláshoz szükséges objektum

        // Felület frissítéshez szükséges delegate-ek
        public delegate void UpdateTextCallback(double s);
        public delegate void UpdateABSCallBack(Visibility v);
        public delegate void UpdateIndexCallBack(Visibility v);
        public delegate void UpdateBGCallBack(double y);
        public delegate void UpdateSteeringWheelCallBack(double y);
        public delegate void UpdateMyCarCallBack(int movement);

        private void SpeedControl()                     // 1000/30 ezredmásodpercenként frissülő thread az akadásmentes grafikai megjelenítéshez
        {

            while (true)                                // Az alkalmazás futása alatt folyamatosan működik
            {

                if (SpeedChange)                        // Megvizsgáljuk tötrénkt-e olyan esemény ami ferfolyásolja a sebességünket
                {
                    if (MaxSpeed < SpeedToReach)
                    {
                        SpeedToReach = MaxSpeed;
                        SpeedChange = false;
                    }
                    else if (MaxSpeed > SpeedToReach)
                    {
                        SpeedToReach = k;
                    }
                }

                if (Speed != SpeedToReach)
                {
                    if (Speed < SpeedToReach)           // Gyorsítás
                    {
                        // Gyorsulási függvény
                        double hanyados = 85 / (180 / SpeedToReach);
                        A = (-((Speed / hanyados) * (Speed / hanyados)) + 5) * MaxAcceleration;
                        A = A / rate;                   // Gyorsítás megfelelő léptkre csökkentése a képfrissítéshez mérten

                        if (A > (SpeedToReach - Speed) / 3) // Erős rángatás elkerülése
                        {
                            A = (SpeedToReach - Speed) / 3;
                        }

                        Speed += A;

                        // Háttér mozgatása az erőhatások egyszerű demonstrálása érdekében
                        yChange = (A * -rate);
                        BG.Dispatcher.Invoke(
                            new UpdateBGCallBack(this.UpdateBG),
                            new object[] { yChange });
                    }
                    else                                // Lassítás
                    {
                        Dec = Speed - SpeedToReach;
                        Dec = Dec / rate;               // Lassulás egységnyi időben


                        if (Dec < maxDec)               
                        {

                            Speed -= Dec;
                            // Háttér mozgatása az erőhatások egyszerű demonstrálása érdekében
                            yChange = (Dec * rate);
                            BG.Dispatcher.Invoke(
                                new UpdateBGCallBack(this.UpdateBG),
                                new object[] { yChange });

                            // ABS működtetése amennyiben szükséges
                            ABS.Dispatcher.Invoke(
                            new UpdateABSCallBack(this.UpdateABS),
                            new object[] { Visibility.Hidden }
                            );
                        }
                        else                            // Maximális lassulás, ha az egységnyi időben történő lassulás meghaladná a maximális értéket
                        {
                            Speed -= maxDec;

                            // Háttér mozgatása az erőhatások egyszerű demonstrálása érdekében
                            yChange = (maxDec * rate);
                            BG.Dispatcher.Invoke(
                                new UpdateBGCallBack(this.UpdateBG),
                                new object[] { yChange });

                            // ABS működtetése amennyiben szükséges
                            ABS.Dispatcher.Invoke(
                            new UpdateABSCallBack(this.UpdateABS),
                            new object[] { Visibility.Visible }
                            );
                        }
                        Dec = 0;
                    }
                }

                // Mivel a sávváltást is akadásmentesen szerettük volna kiviitelezni, ezért ennek a vezérlése is ide került
                // Relatív helymeghatározást kell alkalmaznunk ebben a környeztben
                if (Speed > 0)
                {
                    if (myCarPosition < TargetPosition)
                    {
                        myCarPosition += 1.25;
                    }
                    if (myCarPosition > TargetPosition)
                    {
                        myCarPosition -= 1.25;
                    }
                    if (myCarPosition == TargetPosition)
                    {
                        myLinePrev = myLine;
                    }
                }

                Thread.Sleep((int)(millisecs / rate));          // Szál altatása, 30 képfrissítés másodpercenként

                // Az UdpateText egyszerre tönn dolgot is elvégez, itt hívjuk meg
                MySpeed.Dispatcher.Invoke(
                    new UpdateTextCallback(this.UpdateText),
                    new object[] { Speed }
                );

                // A kormánymozdulat akadásmentes működése érdekében szintén innen van vezérelve
                SteeringWheel.Dispatcher.Invoke(
                    new UpdateSteeringWheelCallBack(this.UpdateSteeringWheel),
                    new object[] { SteeringAngle }
                );
            }
        }

        private void TimingThread()                             // 500 ezredmásodpercentént rissülű thread a háttér számításokhoz
        {
            while (true)                                        // Az alkalmazás futása alatt folyamatosan működik
            {
                if (index != 0)                                 // Megnézzük, hogy éppen van-e folyamatban lévő irányjelzés
                {
                    if (IndexCounter % 2 == 0)                  // Páros másodpercekben látható
                    {
                        RightIndexIndicator.Dispatcher.Invoke(
                            new UpdateIndexCallBack(this.UpdateIndex),
                            new object[] { Visibility.Visible });
                        IndexCounter += 1;
                    }
                    else                                        // Páratlan másodpercekben nem látható
                    {
                        RightIndexIndicator.Dispatcher.Invoke(
                            new UpdateIndexCallBack(this.UpdateIndex),
                            new object[] { Visibility.Hidden });
                        IndexCounter += 1;
                    }
                }

                // Akkumjlátor merülése, sajnos nem jutott idő arra, hogy a kégkondícionálás és egyéb tényezők befolyássolják, így csak a megtett kilométer van rá hatással
                double BatteryDrain = Speed / ((MaxRange / Speed) * 60 * 60 * 2 / SpeedUp);
                if (Battery - BatteryDrain < 0)                 // Teljesen lemerülő akkumulátor esetén az autó leáll, így a szimuláció is
                {
                    System.Environment.Exit(0);
                }
                else
                {
                    Battery -= BatteryDrain;
                }

                // Külső hőmérséklet változása
                OutsideTemp -= (random.NextDouble() * TempStep*2) - TempStep;

                // Belső hőmérséklet változása a külső hőmérséklet és klímabeállítás alapján
                InsideTemp += (OutsideTemp - InsideTemp) / 5;
                InsideTemp += ((ClimateTarget - InsideTemp) / 3) > 0.5 ? 0.5 : ((ClimateTarget - InsideTemp) / 3);

                // Forgalmi sávok foglaltságának eghatársozása
                // Természetesen a saját sávunkban nem lehet másik autó (velünk azonos helyzetben), illetve sávváltás esetén sem érkezhet mellénk autó amíg el nem hagyjuk teljesen a sávot
                // lines_cnt[0] = random.Next(6, 10) megad egy értéket, amit visszaszámlálunk
                // lines_b[0] = NextBool() megad egy igaz vagy hamis értéket, hogy a fentebbi érték visszaszámlálása alatt van-e autó az adott helyen vagy sem
                if (myLine != 1 && myLinePrev != 1)
                {
                    if (lines_cnt[0] == 0)
                    {
                        lines_cnt[0] = random.Next(6, 10);
                        lines_b[0] = NextBool();
                    }
                    else
                    {
                        lines_cnt[0] -= 1;
                    }
                }
                if (myLine != 2 && myLinePrev != 2)
                {
                    if (lines_cnt[1] == 0)
                    {
                        lines_cnt[1] = random.Next(6, 10);
                        lines_b[1] = NextBool();
                    }
                    else
                    {
                        lines_cnt[1] -= 1;
                    }
                }
                if (myLine != 3 && myLinePrev != 3)
                {
                    if (lines_cnt[2] == 0)
                    {
                        lines_cnt[2] = random.Next(6, 10);
                        lines_b[2] = NextBool();
                    }
                    else
                    {
                        lines_cnt[2] -= 1;
                    }
                }

                Thread.Sleep(500);
                if (IndexCounter == 6)                          // Ha már felvillant háromszor az irányjelző, félmásodperenként váltakozva, kikapcsoljuk
                {
                    IndexCounter = 0;
                    index = 0;
                }
            }
        }

        private void SetIndex(int direction)                    // Irányjelzés bekapcsolása és irányának megadása
        {
            index = direction;
        }

        // Annak érdekében, hogy minnél kisebb legyen a kód terjedelme és kevesebb erőforrást igényeljen
        // a lehetőségek szerint csoportosítottuk az azonosan frissülő felületek kezelését
        private void UpdateText(double s)
        {
            // Label tartalom frissítés
            MySpeed.Content = (int)s + " km/h";
            BatteryLabel.Content = (int)Battery + " %";
            Range = (int)(MaxRange * (Battery / 100));
            RangeLabel.Content = Range + " km";

            OutsideTemperatureLabel.Content = Math.Round(OutsideTemp, 1);
            InsideTemperatureLabel.Content = Math.Round(InsideTemp, 1);

            SpeedUpLabel.Content = SpeedUp;

            // Geometriai transzformációs nehézségek miatt az analóg sebességmérőt kettő különböző mutatóval oldottuk meg
            if (s < 90)
            {
                Mutato1.Visibility = Visibility.Visible;
                Mutato2.Visibility = Visibility.Hidden;
                Mutato1.LayoutTransform = new RotateTransform(s);
            }
            else
            {
                Mutato2.Visibility = Visibility.Visible;
                Mutato1.Visibility = Visibility.Hidden;
                Mutato2.LayoutTransform = new RotateTransform(s - 90);
            }

            // Akkumulátor töltöttségi szintjelzése kezdetleges módon
            if (Battery < 80)
            {
                Battery5.Visibility = Visibility.Hidden;
                Battery4.Visibility = Visibility.Visible;
            }
            if (Battery < 60)
            {
                Battery4.Visibility = Visibility.Hidden;
                Battery3.Visibility = Visibility.Visible;
            }
            if (Battery < 40)
            {
                Battery3.Visibility = Visibility.Hidden;
                Battery2.Visibility = Visibility.Visible;
            }
            if (Battery < 20)
            {
                Battery2.Visibility = Visibility.Hidden;
                Battery1.Visibility = Visibility.Visible;
            }
            if (Battery < 10)
            {
                Battery1.Visibility = Visibility.Hidden;
                Battery0.Visibility = Visibility.Visible;
            }

            // Sávokban tartózkodó járművek kezelése
            if (lines_b[0] == true)
            {
                AnotherCar_01.Visibility = Visibility.Visible;
            }
            else
            {
                AnotherCar_01.Visibility = Visibility.Hidden;
            }

            if (lines_b[1] == true)
            {
                AnotherCar_02.Visibility = Visibility.Visible;
            }
            else
            {
                AnotherCar_02.Visibility = Visibility.Hidden;
            }

            if (lines_b[2] == true)
            {
                AnotherCar_03.Visibility = Visibility.Visible;
            }
            else
            {
                AnotherCar_03.Visibility = Visibility.Hidden;
            }

            // Autónk helyzetének változtatása, relatív helymegadás alkalmazása lehetsséges ebben a környezetben
            MyCar.RenderTransform = new TranslateTransform(myCarPosition, 0.0);
        }

        // ABS működtetése
        private void UpdateABS(Visibility v)
        {
            ABS.Visibility = v;
        }

        // Háttér mozgatása az erőhatások egyszerű demonstrálása érdekében
        private void UpdateBG(double y)
        {
            BG.RenderTransform = new TranslateTransform((double)0.0, -y);
        }

        // Kormánymozdulat megjelenítése
        private void UpdateSteeringWheel(double SteeringAngle)
        {
            // Sávváltás esetén a kormánymozdulatot egy szinus függvényre illesztettük, a sávváltáshoz szükséges mozdulat és korrekció megközelítő demonstrákásához
            if (myCarPosition < TargetPosition)
            {
                SteeringAngle += 3*(Math.Sin((myCarPosition - TargetPosition) /20));
            }
            if (myCarPosition > TargetPosition)
            {
                SteeringAngle -= 3 * (Math.Sin((TargetPosition - myCarPosition) / 20));
            }
            SteeringWheel.LayoutTransform = new RotateTransform((double)SteeringAngle);

            // Kanyarkövető fényszórók működtetése
            if (SteeringAngle > 10)
            {
                RightTurningLight.Visibility = Visibility.Visible;
            }
            if(SteeringAngle < -10)
            {
                LeftTurningLight.Visibility = Visibility.Visible;
            }

            if (SteeringAngle < 10)
            {
                RightTurningLight.Visibility = Visibility.Hidden;
            }
            if (SteeringAngle > -10)
            {
                LeftTurningLight.Visibility = Visibility.Hidden;
            }
        }
        
        // Irányjelzés visszajelzése
        private void UpdateIndex(Visibility v)
        {
            if (index < 0)
            {
                LeftIndexIndicator.Visibility = v;
            }
            else
            {
                RightIndexIndicator.Visibility = v;
            }

            // Holttérfigyelő rendszer visszajelzése
            if (!IsLineChangePossible)
            {
                BlindSpotIndicator.Visibility = v;
            }
        }

        // Sebességkorlátozó ikonok elrejtése változás esetén
        public void HideAllSigns()
        {
            _50Sign.Visibility = Visibility.Hidden;
            _90Sign.Visibility = Visibility.Hidden;
            _100Sign.Visibility = Visibility.Hidden;
            _130Sign.Visibility = Visibility.Hidden;
        }

        // Random igaz/hamis érték generálása
        public bool NextBool()
        {
            if (random.Next(0, 2) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public MainWindow()
        {
            
            WindowState = WindowState.Maximized;                // Abalak megnyitása teljes méretben
            InitializeComponent();
            FollowedSpeed.Content = 0;                          // Előttünk haladó / tempomat kezdő értéke

            Mutato1.Visibility = Visibility.Visible;            // 90 km/h alatti mutató látható
            Mutato2.Visibility = Visibility.Hidden;             // 90 km/h feletti mutató elrejtve

            RightTurningLight.Visibility = Visibility.Hidden;   // Kanyarkövető lámpák visszajelzői kikapcsolva
            LeftTurningLight.Visibility = Visibility.Hidden;

            Battery0.Visibility = Visibility.Hidden;            // Csak a maximális akkumulátor töltöttséget jelző ikon látszik
            Battery1.Visibility = Visibility.Hidden;
            Battery2.Visibility = Visibility.Hidden;
            Battery3.Visibility = Visibility.Hidden;
            Battery4.Visibility = Visibility.Hidden;
            Battery5.Visibility = Visibility.Visible;

            AnotherCar_01.Visibility = Visibility.Hidden;       // Forgalomban résztvevő autók kezdéskor elrejtve
            AnotherCar_02.Visibility = Visibility.Hidden;
            AnotherCar_03.Visibility = Visibility.Hidden;

            HideAllSigns();                                     // Minden sebességkorlátozó ikon elrejtése
            _50Sign.Visibility = Visibility.Visible;            // 50 km/h sebességkorlát induláskor, feltételezzük, hogy lakott területről indulunk ez autónkkal

            ABS.Visibility = Visibility.Hidden;                 // ABS visszajező kikapcsolva
            LeftIndexIndicator.Visibility = Visibility.Hidden;  // Irányjelzés kikapcsolva
            RightIndexIndicator.Visibility = Visibility.Hidden;
            BlindSpotIndicator.Visibility = Visibility.Hidden;  // Holttérfigyelő rendszer visszajelzője elrejtve

            // Folyamatok elindítása
            Thread t = new Thread(new ThreadStart(SpeedControl));
            t.Start();
            MySpeed.Content = Speed;

            Thread timing = new Thread(new ThreadStart(TimingThread));
            timing.Start();

        }

        // Előttünk haladó / tempobat beállítás leolvasása
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            k = (int)Slider.Value;
            FollowedSpeed.Content = k;
            SpeedToReach = k;
            SpeedChange = true;                                 // Sebességváltozás történt
        }

        // Irányjelzések bevitele
        private void LeftIndexButton_Click(object sender, RoutedEventArgs e)
        {
            index = -1;                                         // Irányjezlés irányának megadása
            if (myLine - 1 != 0 && lines_b[myLine - 2] == false)// Ha nem található záróvolan, sem másik autó a bal oldalunkon, sávot válthatunk
            {
                IsLineChangePossible = true;                    // Sávváltás kivitelezhető
                myLine -= 1;                                    // Új formalmi sáv megadása

                // Következő sáv megadása relatívan az autó kezdő állapotához képest
                if (myLine == 1)
                {
                    TargetPosition = -250;
                }
                if (myLine == 2)
                {
                    TargetPosition = -120;
                }
                if (myLine == 3)
                {
                    TargetPosition = 0;
                }
            }
            else
            {
                IsLineChangePossible = false;                   // Sávváltás nem kivitelezhető
            }
        }

        private void RightIndexButton_Click(object sender, RoutedEventArgs e)
        {
            index = 1;                                          // Irányjezlés irányának megadása
            if (myLine + 1 <= lines && lines_b[myLine] == false)// Ha nem található záróvolan, sem másik autó a jobb oldalunkon, sávot válthatunk
            {
                IsLineChangePossible = true;                    // Sávváltás kivitelezhető
                myLine += 1;                                    // Új formalmi sáv megadása

                // Következő sáv megadása relatívan az autó kezdő állapotához képest
                if (myLine == 1)
                {
                    TargetPosition = -250;
                }
                if (myLine == 2)
                {
                    TargetPosition = -120;
                }
                if (myLine == 3)
                {
                    TargetPosition = 0;
                }
            }
            else
            {
                IsLineChangePossible = false;                   // Sávváltás nem kivitelezhető
            }
            
        }

        // Táblafelismerő rendzser "kézi működtetése"
        // Sebességkorlátozások bevitele
        private void Button_130_Click(object sender, RoutedEventArgs e)
        {
            MaxSpeed = 130;                                     // Maximális sebesség beállítása
            SpeedChange = true;                                 // Sebességváltozás történt
            HideAllSigns();                                     // Minden sebességkorlátozó ikon elrejtése
            _130Sign.Visibility = Visibility.Visible;           // Megfelelő sebességkorlátozó ikon megjelenítése
        }

        private void Button_100_Click(object sender, RoutedEventArgs e)
        {
            MaxSpeed = 100;
            SpeedChange = true;
            HideAllSigns();
            _100Sign.Visibility = Visibility.Visible;
        }

        private void Button_90_Click(object sender, RoutedEventArgs e)
        {
            MaxSpeed = 90;
            SpeedChange = true;
            HideAllSigns();
            _90Sign.Visibility = Visibility.Visible;
        }

        private void Button_50_Click(object sender, RoutedEventArgs e)
        {
            MaxSpeed = 50;
            SpeedChange = true;
            HideAllSigns();
            _50Sign.Visibility = Visibility.Visible;
        }

        private void NoSpeedLimitButton_Click(object sender, RoutedEventArgs e)
        {
            MaxSpeed = 180;
            SpeedChange = true;
            HideAllSigns();
        }

        // Kormánymozdulat bevitele
        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SteeringAngle = (int)SteeringSlider.Value;
            SteeringWheel.Dispatcher.Invoke(
                    new UpdateSteeringWheelCallBack(this.UpdateSteeringWheel),
                    new object[] { SteeringAngle }
                );
        }

        // Légkondícionáló beállításának bevitele
        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ClimateTarget = ClimateSlider.Value;
            ClimateValueLabel.Content = Math.Round(ClimateSlider.Value, 1) ;
        }

        // Kilépés gomb a program futásának megfelelő befejezéséhez
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        // Út megtételének, így az akkumulátor merülásánek és hátralávő megtehető távolság szimulációjának felgyorsítása
        private void SpeedUpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SpeedUp = (int)SpeedUpSlider.Value;
        }
    }
}
