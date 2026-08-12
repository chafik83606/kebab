using UnityEngine;

/// <summary>
/// Template ScriptableObject pour préconfigurer des profils d'employés.
/// Créer via : Assets > Create > Kebab Empire > Employee Data
/// </summary>
[CreateAssetMenu(fileName = "NewEmployee", menuName = "Kebab Empire/Employee Data")]
public class EmployeeData : ScriptableObject
{
    [Header("Identité")]
    public string employeeName = "Employé";
    [TextArea] public string description;

    [Header("Coûts par défaut")]
    public float declaredDailyCost = GameConstants.DECLARED_EMPLOYEE_DAILY_COST;
    public float undeclaredDailyCost = GameConstants.UNDECLARED_EMPLOYEE_DAILY_COST;

    [Header("Productivité")]
    [Tooltip("Bonus de revenu journalier apporté par cet employé")]
    public float revenueBonus = GameConstants.REVENUE_PER_EMPLOYEE;
}
