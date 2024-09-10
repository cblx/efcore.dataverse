namespace Cblx.EntityFrameworkCore.Dataverse;

public class ChoiceOption
{
    public required int Value { get; set; }
    public required string Name { get; set; }

    public ChoiceOption<TEnum> To<TEnum>() where TEnum : struct => new() { Value = (TEnum)(object)Value, Name = Name };
}


public class ChoiceOption<TEnum> where TEnum : struct
{
    public required TEnum Value { get; set; }
    public required string Name { get; set; }
}

