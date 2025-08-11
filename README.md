
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
\[
\xi_t(T) = \xi_0(T) \cdot \exp\left( 
"
\sum_{i=1}^2 \omega_i X_t^{(i)} - \frac{1}{2} \omega_i^2 v_i(t,T) \right)
\]
avec :
- \( X_t^{(i)} \) : processus gaussiens corrélés,
- \( \omega_i \) : pondérations des facteurs,
- \( v_i(t,T) \) : volatilité instantanée des facteurs.

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

**Conclusion** :  
Le modèle de Bergomi offre un cadre robuste pour la valorisation des produits sur la variance et la volatilité,
en particulier lorsque l'on cherche à intégrer des effets de volatilité stochastique et de corrélation dans un
workflow pratique comme Excel.

