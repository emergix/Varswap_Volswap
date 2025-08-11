
# Introduction à la problématique : Modèle de Bergomi pour Variance Swaps et Volatility Swaps

## 1. Contexte

Les **variance swaps** et **volatility swaps** permettent de prendre une exposition directe à la volatilité future d’un actif, sans pari directionnel sur le niveau du sous-jacent.

- **Variance swap (VS)** : payoff à maturité proportionnel à la différence entre la **variance réalisée** et un **strike de variance** (fair strike) fixé au départ.
- **Volatility swap (VolS)** : similaire, mais le payoff est indexé sur la **volatilité réalisée** (racine carrée de la variance réalisée).

## 2. Problématique de pricing

La valorisation requiert :  
1) une **modélisation dynamique de la variance forward** cohérente avec les marchés d’options vanilles,  
2) une **extrapolation/agrégation** sur maturités et strikes non observés,  
3) une prise en compte des **corrélations** et de la **volatilité de la volatilité** (vol-of-vol).

## 3. Modèle de Bergomi (formulation générale)

On travaille sous la mesure risque-neutre. Le sous-jacent suit :
\[
\frac{dS_t}{S_t} = \sqrt{v_t}\, dW_t^{(0)}, 
\qquad v_t := \xi_t(t),
\]
où \(\xi_t(T)\) est la **variance forward** vue à l’instant \(t\) pour maturité \(T\).  
Le modèle de Bergomi spécifie \(\xi_t(T)\) par une exponentielle de processus gaussiens (Volterra) :
\[
\xi_t(T) \;=\; \xi_0(T)\, \exp\!\Bigg(
\sum_{i=1}^n \omega_i \int_0^t K_i(T,s)\, dW_s^{(i)}
\;-\; \tfrac{1}{2}\, \mathrm{Var}\!\Big[\sum_{i=1}^n \omega_i \!\int_0^t \!K_i(T,s)\, dW_s^{(i)}\Big]
\Bigg),
\]
avec :
- \(W^{(i)}\) des brownien(s) corrélés, \(\mathrm{Corr}(dW^{(i)}_t,dW^{(j)}_t)=\rho^{(v)}_{ij}\), et \(\mathrm{Corr}(dW^{(0)}_t,dW^{(i)}_t)=\rho_i\) (génère le skew),
- \(K_i(T,s)\) des **noyaux déterministes** (ex. noyau exponentiel ou fractionnaire),
- \(\omega_i\) les **charges de facteurs** (vol-of-vol),
- le terme de drift assure \( \mathbb{E}[\xi_t(T)]=\xi_0(T) \) (martingale en \(t\) pour tout \(T\)).

En développant la variance du terme gaussien, on peut écrire explicitement le correctif de drift :
\[
\mathrm{Var}\!\Big[\sum_{i=1}^n \omega_i \!\int_0^t \!K_i(T,s)\, dW_s^{(i)}\Big]
= \sum_{i,j=1}^n \omega_i \omega_j \rho^{(v)}_{ij} \int_0^t K_i(T,s)\,K_j(T,s)\, ds.
\]

### 3.1. Paramétrisation 2 facteurs « exponentielle » (pratique)
Un choix classique est 
\[
K_i(T,s)=\mathbf{1}_{\{s\le T\}}\, e^{-\kappa_i (T-s)},
\]
d’où, pour \(t\le T\),
\[
\xi_t(T) = \xi_0(T)\, \exp\!\Bigg(
\sum_{i=1}^2 \omega_i \!\int_0^t \! e^{-\kappa_i (T-s)} dW_s^{(i)}
-\tfrac{1}{2}\!\sum_{i,j=1}^2 \!\omega_i\omega_j\rho^{(v)}_{ij}\!\int_0^t \!e^{-(\kappa_i+\kappa_j)(T-s)} ds
\Bigg).
\]
Le **processus instantané** est \(v_t=\xi_t(t)\).

> Remarque : la version « rough Bergomi » remplace \(K_i\) par un noyau fractionnaire \(K_H(T,s)\propto (T-s)^{H-\frac12}\), \(H<\tfrac12\).

## 4. Variance réalisée et strikes équitables

Soient \(T_1<T_2\) et \(\tau:=T_2-T_1\) (en year fraction). La **variance réalisée** (continue) sur \([T_1,T_2]\) est
\[
\mathrm{RV}_{[T_1,T_2]} \;=\; \frac{1}{\tau}\int_{T_1}^{T_2} v_u\, du
\;\approx\; \frac{A}{\tau}\sum_{k}\big(\ln S_{t_{k+1}}-\ln S_{t_k}\big)^2,
\]
(avec facteur d’annualisation \(A\) en convention marché).

### 4.1. Strike de **variance swap** (modèle‑free / Bergomi)
Sous des hypothèses standard, on a \(\mathbb{E}[v_u]=\xi_0(u)\). Ainsi le **fair strike** est
\[
K_{\mathrm{var}}(T_1,T_2) \;=\; \mathbb{E}\!\big[\mathrm{RV}_{[T_1,T_2]}\big]
\;=\; \frac{1}{\tau}\int_{T_1}^{T_2} \xi_0(u)\, du.
\]
(C’est cohérent avec la formule « model‑free » via intégrale sur la surface d’options.)

### 4.2. Strike de **volatility swap** (convexity adjustment)
Le **fair strike** du vol‑swap est
\[
K_{\mathrm{vol}}(T_1,T_2) \;=\; \mathbb{E}\!\Big[\sqrt{\mathrm{RV}_{[T_1,T_2]}}\Big]
\;<\; \sqrt{K_{\mathrm{var}}(T_1,T_2)} \quad (\text{Jensen}).
\]
Une approximation de second ordre (petite vol‑of‑vol) donne :
\[
K_{\mathrm{vol}} \;\approx\; \sqrt{\mu}\;-\;\frac{\sigma^2_{\mathrm{RV}}}{8\,\mu^{3/2}},
\qquad
\mu:=K_{\mathrm{var}},\quad
\sigma^2_{\mathrm{RV}}:=\mathrm{Var}\!\big(\mathrm{RV}_{[T_1,T_2]}\big).
\]
Dans Bergomi,
\[
\sigma^2_{\mathrm{RV}} \;=\; \frac{1}{\tau^2}\int_{T_1}^{T_2}\!\!\int_{T_1}^{T_2}
\mathrm{Cov}(v_u,v_s)\, du\, ds,
\]
avec, par log‑normalité de \(v_t\) conditionnelle à \(\mathcal{F}_0\),
\[
\mathrm{Cov}(v_u,v_s)
= \xi_0(u)\,\xi_0(s)\,\Big(\exp\big(C(u,s)\big)-1\Big),
\]
où
\[
C(u,s) \;=\; \sum_{i,j=1}^n \omega_i\omega_j \rho^{(v)}_{ij}
\int_0^{\min(u,s)} K_i(u,t)\,K_j(s,t)\, dt.
\]
**Cas exponentiel 2 facteurs.** Si \(K_i(T,t)=\mathbf{1}_{\{t\le T\}}e^{-\kappa_i(T-t)}\), alors pour \(u\le s\) :
\[
\int_0^{u} e^{-\kappa_i(u-t)} e^{-\kappa_j(s-t)} dt
= \frac{e^{-\kappa_i u-\kappa_j s}}{\kappa_i+\kappa_j}\Big(e^{(\kappa_i+\kappa_j)u}-1\Big)
= \frac{1 - e^{-(\kappa_i+\kappa_j)u}}{\kappa_i+\kappa_j}\, e^{-\kappa_j (s-u)}.
\]
En le combinant pour \(u\le s\) et \(s\le u\), on obtient \(C(u,s)\) et donc \(\sigma^2_{\mathrm{RV}}\).

> Approximations utiles : si \(C(u,s)\) est « petit », \(\exp(C)-1\simeq C\), ce qui simplifie le calcul.

## 5. Options sur variance et sur « square‑root » (lien VolS)

- **Option sur variance (call/put sur \(\mathrm{RV}\) ou sur le strike de VS)** : on peut **approcher \(\mathrm{RV}\) par une log‑normale** via appariement des deux premiers moments \((\mu,\sigma^2_{\mathrm{RV}})\).  
  Soit \(s_{\ln}^2 := \ln\!\big(1+\sigma^2_{\mathrm{RV}}/\mu^2\big)\). Une approximation de type Black donne
\[
\text{Price}\big((\mathrm{RV}-K)^+\big) \approx D \Big(\mu\,\Phi(d_1) - K\,\Phi(d_2)\Big),
\quad
d_{1,2}=\frac{\ln(\mu/K)\pm \tfrac12 s_{\ln}^2}{s_{\ln}},
\]
où \(D\) est l’actualisation (souvent \(D\approx1\) pour un swap), \(\Phi\) la CDF normale.

- **Option « square‑root »** avec payoff \((\sqrt{\mathrm{RV}}-K)^+\) (liée au VolS) : on peut  
  (i) utiliser l’approximation au second ordre \( \sqrt{\mathrm{RV}} \approx \sqrt{\mu} + \frac{\mathrm{RV}-\mu}{2\sqrt{\mu}} - \frac{(\mathrm{RV}-\mu)^2}{8\mu^{3/2}}\) pour obtenir une **vol implicite** effective, ou  
  (ii) approximer directement la loi de \(\sqrt{\mathrm{RV}}\) par log‑normale en appariant ses moments via la **méthode delta**.

## 6. Unités et conventions (prêts pour Excel)

- **Variance** en *unités décimales annualisées* : si la vol implicite est saisie en \(\%\), convertir via \(\sigma^2 = (\mathrm{vol}\%/100)^2\).  
- **Agrégation** : \(K_{\mathrm{var}} = \tau^{-1}\int_{T_1}^{T_2} \xi_0(u)\,du\) \(\rightarrow\) calcul discret \(\sum \xi_0(t_k)\Delta t_k/\tau\).  
- **Moneyness** pour le smile de VS (paramétrisation parabolique utilisée en pratique) : par ex.
\[
\sigma_{\mathrm{VS}}(m;T) = a(T) + b(T)\,m + c(T)\,m^2,\quad
m=\ln\!\big(K_{\mathrm{var}}(T_1,T_2)/K\big)\ \text{ou}\ m=K/K_{\mathrm{var}}-1.
\]
Veiller à la **positivité** et à l’absence d’**arbitrage de calendrier** lors du fit.

---

**En résumé** :  
- Dans Bergomi, \(K_{\mathrm{var}}\) est entièrement déterminé par la courbe \(\xi_0\).  
- \(K_{\mathrm{vol}}\) requiert la **covariance** de \(v_t\) (dépend de \(\omega_i\), \(K_i\), \(\rho^{(v)}\)).  
- Les options (sur variance ou « square‑root ») se traitent efficacement par **appariement de moments** et formules de type **Black**.
