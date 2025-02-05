using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Poker
{
    class Program
    {
        // -----------------------
        // DECLARATION DES DONNEES
        // -----------------------
        // Importation des DLL (librairies de code) permettant de gérer les couleurs en mode console
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(uint nStdHandle);
        static uint STD_OUTPUT_HANDLE = 0xfffffff5;
        static IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        // Pour utiliser la fonction C 'getchar()' : sasie d'un caractère
        [DllImport("msvcrt")]
        static extern int _getche();

        //-------------------
        // TYPES DE DONNEES
        //-------------------

        // Fin du jeu
        public static bool fin = false;

        // Codes COULEUR
        public enum couleur { VERT = 10, ROUGE = 12, JAUNE = 14, BLANC = 15, NOIRE = 0, ROUGESURBLANC = 252, NOIRESURBLANC = 240 };

        // Coordonnées pour l'affichage
        public struct coordonnees
        {
            public int x;
            public int y;
        }

        // Une carte    
        public struct carte
        {
            public char valeur;
            public int famille;
        };

        // Liste des combinaisons possibles
        public enum combinaison { CARTE_HAUTE, PAIRE, DOUBLE_PAIRE, BRELAN, QUINTE, FULL, COULEUR, CARRE, QUINTE_FLUSH };

        // Valeurs des cartes : As, Roi,...
        public static char[] valeurs = { 'A', 'R', 'D', 'V', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        // Codes ASCII (3 : coeur, 4 : carreau, 5 : trèfle, 6 : pique)
        public static int[] familles = { 3, 4, 5, 6 };

        // Numéros des cartes à échanger
        public static int[] echange = { 0, 0, 0, 0 };

        // Jeu de 5 cartes
        public static carte[] MonJeu = new carte[5];

        //----------
        // FONCTIONS
        //----------

        // Génère aléatoirement une carte : {valeur;famille}
        // Retourne une expression de type "structure carte"
        public static carte tirage()
        {
            Random r = new Random();
            carte uneCarte = default(carte);
            uneCarte.valeur = valeurs[r.Next(0, 13)];
            uneCarte.famille = familles[r.Next(0, 4)];
            return uneCarte;
        }

        // Indique si une carte est déjà présente dans le jeu
        // Paramètres : une carte, le jeu 5 cartes, le numéro de la carte dans le jeu
        // Retourne un entier (booléen)
        // fonction carteunique qui change la valeur du booléen si une carte est déjà existante lors du tirage 
        // ou du remplacement de la carte dans l'état actuel
        public static bool carteUnique(carte uneCarte, carte[] unJeu, int numero)
        {
            {
                int i = 0;
                bool unique = true;
                checked
                {
                    do
                    {
                        if (i != numero)
                        {
                            if (uneCarte.valeur == unJeu[i].valeur && uneCarte.famille == unJeu[i].famille)
                            {
                                unique = false;
                            }
                            else
                            {
                                i++;
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }
                    while (unique && i < 5);
                    return unique;
                }
            }
        }

        // Calcule et retourne la COMBINAISON (paire, double-paire... , quinte-flush)
        // pour un jeu complet de 5 cartes.
        // La valeur retournée est un élement de l'énumération 'combinaison' (=constante)
        public static combinaison cherche_combinaison(carte[] unJeu)
        {
            int nbpaires = 0; // int paires
            int[] array = new int[5];
            int[] similaire = array; // Cartes similaires
            bool paire = false; // bool pour bloquer une paire dans le jeu
            bool brelan = false; // bool pour bloquer un brelan dans le jeu, les deux bools permettent
            char[,] quintes = new char[4, 5] // de verrouiller une paire/doublepaire/full
            {
            { 'X', 'V', 'D', 'R', 'A' },
            { '9', 'X', 'V', 'D', 'R' },
            { '8', '9', 'X', 'V', 'D' },
            { '7', '8', '9', 'X', 'V' }
            };
            combinaison resultat = combinaison.CARTE_HAUTE;
            checked
            {
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        if (unJeu[i].valeur == unJeu[j].valeur)
                        {
                            similaire[i]++;
                        }
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    if (similaire[i] == 4) // si 4 cartes identiques = carré
                    {
                        return combinaison.CARRE;
                    }
                    if (similaire[i] == 3) // si 3 cartes identiques = brelan
                    {
                        resultat = combinaison.BRELAN;
                        brelan = true;
                    }
                    else if (similaire[i] == 2) // si 2 cartes identiques = 1 paire
                    {
                        resultat = combinaison.PAIRE;
                        paire = true;
                        nbpaires++;
                    }
                }
                nbpaires = unchecked(nbpaires / 2); // si 2x 2 cartes identiques = double paire
                if (nbpaires == 2)
                {
                    return combinaison.DOUBLE_PAIRE;
                }
                if (paire && brelan) // si 1 paire + 1 brelan = full
                {
                    return combinaison.FULL;
                }
                if (similaire[0] + similaire[1] + similaire[2] + similaire[3] + similaire[4] == 5) // si valeurs se suivent = quinte
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int nb = 0;
                        for (int j = 0; j < 5; j++)
                        {
                            if (unJeu[j].valeur == quintes[i, 0] || unJeu[j].valeur == quintes[i, 1] || unJeu[j].valeur == quintes[i, 2] || unJeu[j].valeur == quintes[i, 3] || unJeu[j].valeur == quintes[i, 4])
                            {
                                nb++;
                            }
                        }
                        if (nb == 5)
                        {
                            resultat = combinaison.QUINTE; // si suite + couleur, alors suite = quinte flush 
                            if (unJeu[0].famille == unJeu[1].famille && unJeu[1].famille == unJeu[2].famille && unJeu[2].famille == unJeu[3].famille && unJeu[3].famille == unJeu[4].famille)
                            {
                                return combinaison.QUINTE_FLUSH;
                            }
                            break;
                        }
                    }
                }
                if (unJeu[0].famille == unJeu[1].famille && unJeu[1].famille == unJeu[2].famille && unJeu[2].famille == unJeu[3].famille && unJeu[3].famille == unJeu[4].famille)
                {
                    resultat = combinaison.COULEUR; // si 5x même couleur peu importe valeur alors = couleur
                }
                return resultat;
            }
        }

        // Echange des cartes
        // Paramètres : le tableau de 5 cartes et le tableau des numéros des cartes à échanger
        private static void echangeCarte(carte[] unJeu, int[] e)
        {
            try
            {
                for (int i = 0; i < e.Length; i = checked(i + 1))
                {
                    do
                    {
                        ref carte reference = ref MonJeu[e[i]];
                        reference = tirage();
                    }
                    while (!carteUnique(MonJeu[e[i]], MonJeu, e[i]));
                    affichageCarte(unJeu[i]);
                }
            }
            catch
            {
            }
        }

        // Pour afficher le Menu pricipal
        private static void afficheMenu()
        {
            Console.Clear();
            SetConsoleTextAttribute(hConsole, 11);
            Console.WriteLine("1) Jouer au Poker");
            Console.WriteLine("2) Consulter les high scores");
            Console.WriteLine("3) Quitter");
        }

        // Tirage d'un jeu de 5 cartes
        // Paramètre : le tableau de 5 cartes à remplir
        private static void tirageDuJeu(carte[] MonJeu)
        {
            for (int i = 0; i < 5; i = checked(i + 1))
            {
                do
                {
                    ref carte reference = ref MonJeu[i];
                    reference = tirage();
                }
                while (!carteUnique(MonJeu[i], MonJeu, i));
            }
        }

        // Affiche à l'écran une carte {valeur;famille} 
        private static void affichageCarte(carte uneCarte)
        {
            //----------------------------
            // TIRAGE D'UN JEU DE 5 CARTES
            //----------------------------
            int left = 0;
            int c = 1;
            // Tirage aléatoire de 5 cartes
            for (int i = 0; i < 5; i++)
            {
                // Tirage de la carte n°i (le jeu doit être sans doublons !)

                // Affichage de la carte en array famille et valeur
                if (MonJeu[i].famille == 3 || MonJeu[i].famille == 4)
                    SetConsoleTextAttribute(hConsole, 240);
                else
                    SetConsoleTextAttribute(hConsole, 252);
                Console.SetCursorPosition(left, 5);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '*', '-', '-', '-', '-', '-', '-', '-', '-', '-', '*');
                Console.SetCursorPosition(left, 6);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 7);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 8);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', ' ', ' ', ' ', ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 9);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 10);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 11);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 12);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', ' ', ' ', ' ', ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 13);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 14);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 15);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '*', '-', '-', '-', '-', '-', '-', '-', '-', '-', '*');
                Console.SetCursorPosition(left, 16);
                SetConsoleTextAttribute(hConsole, 10);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", ' ', ' ', ' ', ' ', ' ', c, ' ', ' ', ' ', ' ', ' ');
                left = left + 15;
                c++;
            }

        }
        //--------------------
        // Fonction PRINCIPALE
        //--------------------

        private static void Main(string[] args)
        {
            {
                string reponse; // boucle do pour afficher le menu
                do
                {
                    Console.Clear();
                    afficheMenu();
                    do
                    {
                        SetConsoleTextAttribute(hConsole, 7);
                        Console.Write("Votre choix : "); // entrée utilisateur pour les réponses 1/2/3
                        reponse = Console.ReadLine();
                    }
                    while (reponse != "1" && reponse != "2" && reponse != "3"); // gestion de réponse nulle pour rafficher le menu
                    Console.Clear();
                    string nom;
                    if (reponse == "1") // commencement du jeu si réponse = 1
                    {
                        int i = 0;
                        tirageDuJeu(MonJeu); // tirage du jeu 
                        affichageCarte(MonJeu[i]); // affichage du jeu
                        try
                        {
                            int compteur = 0;
                            Console.Write("Nombre de cartes a echanger <0-5> ? : ");
                            compteur = int.Parse(Console.ReadLine());
                            int[] e = new int[compteur];
                            for (int j = 0; j < e.Length; j++)
                            {
                                Console.Write("Carte <1-5> : ");
                                e[j] = int.Parse(Console.ReadLine());
                                e[j]--; // -- car 0 - 4 dans le programme contrairement à 1 - 5 "humainement"
                            }
                            echangeCarte(MonJeu, e);
                        }
                        catch
                        {
                        }
                        Console.Clear();
                        affichageCarte(MonJeu[i]);
                        Console.Write("RESULTAT - Vous avez : "); // try catch pour délimiter la fin du jeu et l'enregistrement du high score
                        try
                        {
                            switch (cherche_combinaison(MonJeu)) // switch case des différentes combinaisons
                            {
                                case combinaison.CARTE_HAUTE:
                                    Console.WriteLine("Une carte haute ? Vous allez pas aller loin.");
                                    break;
                                case combinaison.PAIRE:
                                    Console.WriteLine("Une paire...");
                                    break;
                                case combinaison.DOUBLE_PAIRE:
                                    Console.WriteLine("Deux fois mieux qu'une paire.");
                                    break;
                                case combinaison.BRELAN:
                                    Console.WriteLine("Un brelan, pas mal !");
                                    break;
                                case combinaison.QUINTE:
                                    Console.WriteLine("Une quinte !");
                                    break;
                                case combinaison.FULL:
                                    Console.WriteLine("Un full ! On peut difficilement rêver mieux !");
                                    break;
                                case combinaison.COULEUR:
                                    Console.WriteLine("Une couleur !");
                                    break;
                                case combinaison.CARRE:
                                    Console.WriteLine("Les quatre au complet ! Un carré !");
                                    break;
                                case combinaison.QUINTE_FLUSH:
                                    Console.WriteLine("une quinte-flush; royal!");
                                    break;
                            }
                        }
                        catch
                        {
                        }
                        Console.ReadKey();
                        nom = "";
                        Console.Write("Enregistrer le Jeu ? (O/N) : ");
                        char enregister = char.Parse(Console.ReadLine()); // enregistrement et mise en capitale pour reconnaître 'O'
                        enregister = char.ToUpper(enregister);
                        if (enregister == 'O')
                        {
                            Console.WriteLine("Vous pouvez saisir votre nom (ou pseudo) : "); // entrée utilisateur 
                            nom = Console.ReadLine();
                            BinaryWriter f2;
                            using (f2 = new BinaryWriter(new FileStream("scores.txt", FileMode.Append, FileAccess.Write)))
                            { // objet binary writer pour créer un flux de texte à enregisterr
                                f2.Write(nom);
                                f2.Write(MonJeu[0].famille);
                                f2.Write(MonJeu[0].valeur);
                                f2.Write(MonJeu[1].famille);
                                f2.Write(MonJeu[1].valeur);
                                f2.Write(MonJeu[2].famille);
                                f2.Write(MonJeu[2].valeur);
                                f2.Write(MonJeu[3].famille);
                                f2.Write(MonJeu[3].valeur);
                                f2.Write(MonJeu[4].famille);
                                f2.Write(MonJeu[4].valeur);
                                f2.Flush();
                            }
                        }
                    }
                    if (!(reponse == "2")) // réponse 2 du menu --> high score
                    {
                        continue;
                    }
                    char[] delimiteur = new char[1] { ';' };
                    if (File.Exists("scores.txt")) // vérification de l'existence de scores.txt
                    {
                        using (BinaryReader f = new BinaryReader(new FileStream("scores.txt", FileMode.Open, FileAccess.Read)))
                        {
                            nom = f.ReadString();
                        }
                        Console.WriteLine("Nom : " + nom);
                        Console.ReadKey();
                    }
                }
                while (!(reponse == "3")); // si réponse 3 (fermer) terminer programme
                Console.Clear();
            }
        }
    }
}