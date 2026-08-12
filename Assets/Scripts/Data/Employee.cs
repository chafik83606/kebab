using System;

/// <summary>
/// Représente un employé d'un restaurant (données runtime sérialisables).
/// </summary>
[Serializable]
public class Employee
{
    public string employeeName;
    public bool isDeclared;       // true = déclaré, false = au black
    public float dailyWage;       // Coût journalier
    public int daysEmployed;      // Ancienneté (jours)

    public Employee() { }

    public Employee(string name, bool declared)
    {
        employeeName = name;
        isDeclared = declared;
        dailyWage = declared
            ? GameConstants.DECLARED_EMPLOYEE_DAILY_COST
            : GameConstants.UNDECLARED_EMPLOYEE_DAILY_COST;
        daysEmployed = 0;
    }

    public string StatusLabel => isDeclared ? "Déclaré" : "Non déclaré";
}
