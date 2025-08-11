
# Introduction à la problématique : Modèle de Bergomi pour Variance Swaps et Volatility Swaps

## 1. Contexte

Les **variance swaps** et **volatility swaps** sont des produits dérivés permettant aux investisseurs de
prendre des positions directes sur la volatilité future d'un actif sous-jacent, sans exposition directe à son prix.

- **Variance swap** : payoff proportionnel à la différence entre la variance réalisée et la variance fixée (strike).
- **Volatility swap** : similaire, mais basé sur la racine carrée de la variance réalisée, donc directement sur la volatilité.

Ces instruments sont utilisés à des fins de couverture ou de spéculation, notamment dans la gestion de portefeuille et le trading de volatilité pure.

## 2. Problématique de pricing

La valorisation de ces produits nécessite :
1. **Une modélisation dynamique de la volatilité** cohérente avec les marchés d'options vanilles.
2. **Une extrapolation des smiles et surfaces de volatilité** aux maturités et strikes non directement observables.
3. **Une gestion des effets de corrélation et de volatilité stochastique**.

## 3. Modèle de Bergomi

Le **modèle de Bergomi** est un modèle de volatilité stochastique non-markovien permettant de reproduire
la dynamique du smile implicite et de capturer la structure de dépendance de la variance à travers le temps.

Dans sa version à deux facteurs, il modélise la variance forward comme :

$$\xi_t(T) = \xi_0(T) \cdot \exp\left( 
"
\sum_{i=1}^2 \omega_i X_t^{(i)} - \frac{1}{2} \omega_i^2 v_i(t,T) \right)$$
avec :
- $$X_t^{(i)}$$ : processus gaussiens corrélés,
- $$\omega_i$$ : pondérations des facteurs,
- $$v_i(t,T)$$ : volatilité instantanée des facteurs.

## Le code fourni en csharp implemente les pricipaux instruments de ce marché avec une calibration du modele 


## 4. Application aux variance swaps et volatility swaps

- **Variance swap** : nécessite d'agréger la variance forward entre deux dates (T1, T2), pondérée selon l'échéance.
- **Volatility swap** : nécessite de calculer l'espérance de la racine de la variance réalisée, ce qui n'est pas linéaire.
- **Options sur variance swap** : nécessitent une distribution complète de la variance réalisée, d'où l'utilisation
d'un modèle comme Bergomi pour simuler ou approximer cette distribution.

## 5. Implémentation Excel-DNA

L'utilisation d'**Excel-DNA** permet :
- Une intégration directe dans un tableur Excel via des **fonctions personnalisées (UDF)**.
- Un accès rapide aux prix et grecs pour différents paramètres du modèle.
- Une flexibilité pour le calibrage aux données de marché et la comparaison avec d'autres modèles.

**Exemple de formule dans Excel :**
```
=Export_VarianceSwap_ForwardVariance(varList; dateList; T1; T2; interpolMethod)
```
où :
- `varList` : liste de variances implicites en %,
- `dateList` : échéances correspondantes,
- `T1`, `T2` : dates de début et de fin,
- `interpolMethod` : méthode d'interpolation choisie.

---


## 1. Structure générale

    Espaces de noms :

        Interpolation : Gère différentes méthodes d'interpolation (linéaire, spline, etc.).

        BergomiTwoFactors : Contient les modèles de volatilité (Bergomi et parabolique).

        BergomiTest : Fournit les fonctions exportables vers Excel via Excel-DNA.

## 2. Composants clés du modèle de Bergomi
# a) Modèle de volatilité parabolique (ParabolicVolatlityModel)

    Objectif : Modéliser la surface de volatilité locale.

    Paramètres :

        SigmaVariance : Volatilité de la variance.

        SigmaSkew : Pente de la skew.

        SigmaCurvature : Courbure (smile).

        SigmaAlpha : Paramètre de term structure.

    Fonctions :

        getVol() : Calcule la volatilité pour un moneyness F0/K et une maturité T.

        ComputeTurningMoneyness() : Trouve le point où le smile change de comportement.

# b) Modèle de Bergomi (BergomiVolatilityModel)

    Paramètres du modèle :

        k1, k2 : Vitesses de retour des facteurs de variance.

        theta : Corrélation entre les facteurs.

        rho : Corrélation spot/variance.

        omega : Volatilité de la volatilité.

    Fonctions :

        getVol() : Calcule la volatilité implicite via BasketVolatility().

## 3. Calculs financiers
# a) Swaps de variance

    Fonctions clés :

        VarianceSwap_ForwardVariance() : Calcule la variance forward entre T1 et T2.

        Bergomi_VarianceSwapVolatility() : Calcule la volatilité d'un swap de variance.

        Export_ParabolicVol_VarianceSwapOption() : Fonction Excel pour pricer une option sur swap de variance.

# b) Swaps de volatilité

    Fonctions clés :

        Bergomi_VarianceToVol() : Transforme la variance en volatilité.

        Export_ParabolicVol_VolSwapOption() : Fonction Excel pour pricer une option sur swap de volatilité.

# c) Options complexes

    Options de type "Square Root" :

        Bergomi_SquareRootCall() : Price une option sur la racine carrée de la variance (pour les volatility swaps).

        Utilise des intégrations numériques (Gauss-Legendre) pour résoudre les formules fermées.

# 4. Intégration Excel (Excel-DNA)

    Fonctions exportées (exemples) :
    csharp

    [ExcelFunction(Description = "BlackSholes Call pricing")]
    public static double Export_Call(double f, double k, double t, double v) { ... }

    [ExcelFunction(Description = "Variance Swap Option pricing")]
    public static double Export_ParabolicVol_VarianceSwapOption(...) { ... }

    Paramètres typiques :

        varlist : Liste des variances.

        datelist : Dates associées.

        T, K : Maturité et strike.

        Paramètres du modèle (k1, k2, theta, etc.).

# 5. Techniques numériques

    Interpolation :

        Splines, linéaire, constante (gérées par InterpolationStructure).

    Intégration :

        Quadrature de Gauss-Legendre (CGaussLegendre) pour les intégrales complexes.

    Calcul de volatilité implicite :

        Méthode de Newton-Raphson dans BlackScholes.ImpliedVolatility().

# 6. Exemple d'utilisation dans Excel
```csharp
=Export_ParabolicVol_VarianceSwapOption(
    0.2,       // SigmaVariance
   -0.1,       // SigmaSkew
    0.05,      // SigmaCurvature
    0.5,       // SigmaAlpha
    A1:A10,    // varlist (variance points)
    B1:B10,    // datelist (dates)
    1.0,       // T (maturity)
    0.22,      // K (strike)
    0.25,      // T1 (start)
    2.0,       // T2 (end)
    0.01,      // dt (time step)
    15,        // NbLegendre (integration points)
    0          // VolOrOption (0=vol, 1=price)
)
```

## Résumé

Le code implémente avec rigueur le modèle de Bergomi à deux facteurs pour :

    Pricer des swaps de variance et de volatilité.

    Calculer les volatilités implicites via des méthodes numériques avancées.

    Exporter ces fonctionnalités vers Excel via Excel-DNA, permettant une utilisation pratique par les traders.



**Conclusion** :  
Le modèle de Bergomi offre un cadre robuste pour la valorisation des produits sur la variance et la volatilité,
en particulier lorsque l'on cherche à intégrer des effets de volatilité stochastique et de corrélation dans un
workflow pratique comme Excel.
