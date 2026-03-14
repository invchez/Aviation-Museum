using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlaneSO", menuName = "Scriptable Objects/PlaneSO")]
public class PlaneSO : ScriptableObject
{
    public string Name;
    public string Information;
    public string Manufacturer;
    public string Date;
    public string WingSpan;
    public string TSpeed;
    public List <FAQ> FAQS;
}
