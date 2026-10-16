using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

public class RaceDayValidationTests
{
	private static bool IsValid(object model)
	{
		var context = new ValidationContext(model);
		var results = new List<ValidationResult>();

		return Validator.TryValidateObject(
			model,
			context,
			results,
			validateAllProperties: true);
	}

	[Fact]
	public void EnrolmentRequest_ValidCategoryId_Passes()
	{
		var request = new EnrolmentRequest
		{
			EventCategoryID = 1
		};

		Assert.True(IsValid(request));
	}

	[Fact]
	public void EnrolmentRequest_ZeroCategoryId_Fails()
	{
		var request = new EnrolmentRequest
		{
			EventCategoryID = 0	
		};

		Assert.False(IsValid(request));
	}

	[Fact]
	public void ResultRequest_ValidFinishedStatus_Passes()
	{
		var request = new ResultRequest
		{
			EnrolmentID = 1,
			FinishTime = new TimeSpan(1, 25, 30),
			Position = 1,
			ResultStatus = "Finished"
		};

		Assert.True(IsValid(request));
	}

	[Fact]
	public void ResultRequest_InvalidStatus_Fails()
	{
		var request = new ResultRequest
		{
			EnrolmentID = 1,
			ResultStatus = "Winner"
		};

		Assert.False(IsValid(request));
	}

	[Fact]
	public void ResultRequest_ZeroPosition_Fails()
	{
		var request = new ResultRequest
		{
			EnrolmentID = 1,
			Position = 0,
			ResultStatus = "Finished"
		};

		Assert.False(IsValid(request));
	}

	[Fact]
	public void ResultRequest_ZeroEnrolmentId_Fails()
	{
		var request = new ResultRequest
		{
			EnrolmentID = 0
		};

		Assert.False(IsValid(request));
	}

	[Fact]
	public void Enrolment_DefaultStatus_IsPending()
	{
		var enrolment = new Enrolment();

		Assert.Equal("Pending", enrolment.Status);
	}

	[Fact]
	public void Result_DefaultStatus_IsFinished()
	{
		var result = new Result();

		Assert.Equal("Finished", result.ResultStatus);
	}

	[Fact]
	public void Result_FinishTime_IsNullable()
	{
		var result = new Result
		{
			EnrolmentID = 1,
			FinishTime = null,
			Position = null,
			ResultStatus = "DNS"
		};

		Assert.Null(result.FinishTime);
		Assert.Null(result.Position);
	}

	[Fact]
	public void EventCategory_EntryFee_CannotBeNegative_InDatabaseModel()
	{
		using var context = CreateContext();

		var entityType = GetDesignTimeEntityType(
		context, typeof(EventCategory));

		Assert.NotNull(entityType);

		var table = entityType.GetCheckConstraints();

		Assert.Contains(table, constraint =>
			constraint.Name == "CK_EventCategories_EntryFee" &&
			constraint.Sql!.Contains("[EntryFee] >= 0"));
	}

	[Fact]
	public void Enrolment_Status_IsRestricted_InDatabaseModel()
	{
		using var context = CreateContext();

		var entityType = GetDesignTimeEntityType(
			context, typeof(Enrolment));

		Assert.NotNull(entityType);

		Assert.Contains(entityType.GetCheckConstraints(),
			constraint =>
				constraint.Name == "CK_Enrolments_Status" &&
				constraint.Sql!.Contains("Pending") &&
				constraint.Sql.Contains("Confirmed") &&
				constraint.Sql.Contains("Cancelled"));
	}

	[Fact]
	public void Result_Status_IsRestricted_InDatabaseModel()
	{
		using var context = CreateContext();

		var entityType = GetDesignTimeEntityType(
			context, typeof(Result));

		Assert.NotNull(entityType);

		Assert.Contains(entityType.GetCheckConstraints(),
			constraint =>
				constraint.Name == "CK_Results_Status" &&
				constraint.Sql!.Contains("Finished") &&
				constraint.Sql.Contains("DNF") &&
				constraint.Sql.Contains("DNS") &&
				constraint.Sql.Contains("DQ"));
	}


	private static IEntityType GetDesignTimeEntityType(
		RaceDayDbContext context,
		Type entityType)
	{
		var designTimeModel = context
			.GetService<IDesignTimeModel>()
			.Model;

		return designTimeModel.FindEntityType(entityType)
			?? throw new InvalidOperationException(
				$"Entity type {entityType.Name} was not found.");
	}


	private static RaceDayDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<RaceDayDbContext>()
			.UseSqlServer(
				"Server=(localdb)\\MSSQLLocalDB;Database=RaceDayTestModel;Trusted_Connection=True;")
			.Options;

		return new RaceDayDbContext(options);
	}
}
