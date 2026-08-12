using UnityEngine;

/// <summary>
/// Gère la liste des employés d'un restaurant.
/// </summary>
public class EmployeeManager : MonoBehaviour
{
    private RestaurantData data;

    public void Bind(RestaurantData restaurantData)
    {
        data = restaurantData;
    }

    public void Hire(bool isDeclared, string employeeName = null)
    {
        if (data == null) return;

        string name = string.IsNullOrEmpty(employeeName)
            ? $"Employé {data.employees.Count + 1}"
            : employeeName;

        data.employees.Add(new Employee(name, isDeclared));
    }

    public void Fire(int index)
    {
        if (data == null || index < 0 || index >= data.employees.Count) return;
        data.employees.RemoveAt(index);
    }

    public int Count => data?.employees.Count ?? 0;
    public int UndeclaredCount => data?.UndeclaredEmployeeCount ?? 0;
}
