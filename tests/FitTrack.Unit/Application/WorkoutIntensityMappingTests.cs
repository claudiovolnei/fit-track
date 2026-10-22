using FitTrack.Application.Mapping;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Unit.Application;

/// <summary>
/// The level of a workout travels as the word the contract names it, not as the number of the enum:
/// the client and the API have to agree on the vocabulary, or the whole sheet of the athlete comes
/// down with a deserialization error.
/// </summary>
public sealed class WorkoutIntensityMappingTests
{
    [Theory]
    [InlineData(WorkoutIntensity.Leve, "leve")]
    [InlineData(WorkoutIntensity.Medio, "medio")]
    [InlineData(WorkoutIntensity.Moderado, "moderado")]
    [InlineData(WorkoutIntensity.Intenso, "intenso")]
    [InlineData(WorkoutIntensity.MuitoIntenso, "muitoIntenso")]
    public void ToContract_NamesTheLevel(WorkoutIntensity intensity, string expected) =>
        Assert.Equal(expected, WorkoutIntensityMapping.ToContract(intensity));

    [Fact]
    public void ToContract_WithoutALevel_SaysNothing() =>
        Assert.Null(WorkoutIntensityMapping.ToContract(null));

    [Theory]
    [InlineData("leve", WorkoutIntensity.Leve)]
    [InlineData("Leve", WorkoutIntensity.Leve)]
    [InlineData("moderado", WorkoutIntensity.Moderado)]
    [InlineData("MUITOINTENSO", WorkoutIntensity.MuitoIntenso)]
    public void FromContract_ReadsTheLevel(string word, WorkoutIntensity expected) =>
        Assert.Equal(expected, WorkoutIntensityMapping.FromContract(word));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("impossível")]
    [InlineData("muito intenso")]
    public void FromContract_AWordOutsideTheList_IsRefused(string? word)
    {
        var exception = Assert.Throws<DomainException>(() => WorkoutIntensityMapping.FromContract(word));

        Assert.Equal("session.finish.intensity.invalid", exception.Code);
    }
}
