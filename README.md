# Kebab Empire

Jeu mobile de gestion / stratégie d'empire commercial (Unity — Android & iOS).

Tu commences avec un petit kebab. Tu gères employés, viandes, hygiène et impôts pour agrandir ton empire… ou te faire racheter.

## Démarrage rapide

1. Ouvre ce dossier comme projet Unity (**2021.3 LTS** ou plus récent, URP ou Built-in).
2. Attends la compilation des scripts.
3. Menu **Kebab Empire → Setup Main Scene**.
4. **File → Save As** → `Assets/Scenes/MainScene.unity`.
5. Appuie sur **Play**.

### Boutons de test

| Action | Effet |
|--------|--------|
| Passer un jour | Revenus, saleté, contrôles aléatoires, IA |
| Toucher un resto | Vue détaillée |
| Faire le ménage | Saleté → 0 |
| Viande Bœuf / Poulet / ??? | Change le type de viande |
| Embaucher déclaré / black | Ajoute un employé |
| Upgrade Grill / Frigo / Vitrine | Améliore le matériel |

## Architecture

```
Assets/
├── Scripts/
│   ├── Managers/       EmpireManager, GameManager, SaveSystem
│   ├── Restaurant/     RestaurantManager, EmployeeManager, StockManager
│   ├── Visuals/        HygieneVisualController, FlyMover
│   ├── Data/           MeatType, RestaurantData, Employee, GameConstants…
│   ├── Competitors/    CompetitorManager
│   ├── UI/             UIManager, RestaurantUI
│   └── Editor/         Setup automatique de la scène
├── Prefabs/
├── ScriptableObjects/
└── Scenes/
```

## Mécaniques incluses

- **3 viandes** : Bœuf > Poulet > Préfère pas savoir (coût, réputation, saleté, risque)
- **Employés** déclarés (150€/j) ou au black (70€/j) + amendes URSSAF
- **Saleté visuelle** : paliers 20% / 50% / 75% (taches, mouches, particules)
- **Matériel** : Grill, Frigo, Vitrine (niveaux 1→3)
- **Impôts** tous les 30 jours + majoration 10%
- **Contrôles** aléatoires (URSSAF + sanitaire)
- **Concurrents IA** : faillite → rachat ; santé Fragile → rachat hostile (Game Over)
- **Sauvegarde JSON** auto (`Application.persistentDataPath/kebab_empire_save.json`)

## ScriptableObjects

Créer via le menu **Assets → Create → Kebab Empire** :

- `Restaurant Template` — modèles de locaux achetable
- `Employee Data` — profils d'employés

## Hygiène visuelle

Sur chaque `Restaurant`, le composant `HygieneVisualController` attend :

- `dirtStains[]` — taches (actif dès 21%)
- `trashItems[]` — déchets (actif dès 51%)
- `flyPrefabs[]` — mouches individuelles (51–75%)
- `flySwarm` — ParticleSystem (76%+)
- `mopObject` — animation de ménage

Clic droit sur le composant → **Test: Propre / Négligé / Crado / Infestation**.

## Build mobile

1. **File → Build Settings** → Android / iOS
2. Orientation recommandée : Portrait
3. UI scalée en 1080×1920 (`CanvasScaler`)

## Équilibrage

Tous les chiffres (salaires, amendes, impôts, prix…) sont centralisés dans :

`Assets/Scripts/Data/GameConstants.cs`
