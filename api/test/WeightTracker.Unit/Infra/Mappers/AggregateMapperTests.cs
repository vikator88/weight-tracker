using FluentAssertions;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using Xunit;

namespace WeightTracker.Unit.Infra.Mappers;

/// <summary>
/// Round trips and corrupt-storage handling for the aggregate mappers other than <c>WorkoutMapper</c>.
/// Corrupt persisted data must surface as a persistence fault, never as a domain error,
/// because it is a server problem rather than bad caller input.
/// </summary>
public class AggregateMapperTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    public class Users
    {
        private static User AUser(Role role = Role.TRAINER) => User.Rehydrate(
            Id.New(), Email.From("trainer@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
            new DateOnly(1985, 12, 9), role, PasswordHash.From("stored-hash"), CreatedAt);

        [Fact]
        public void RoundTrip_ShouldPreserveEveryField()
        {
            // Arrange
            var original = AUser();

            // Act
            var restored = UserMapper.MapToDomain(UserMapper.MapToEntity(original));

            // Assert
            restored.Id.Should().Be(original.Id);
            restored.Email.Should().Be(original.Email);
            restored.Name.Should().Be(original.Name);
            restored.Surname.Should().Be(original.Surname);
            restored.DateBirth.Should().Be(original.DateBirth);
            restored.Role.Should().Be(Role.TRAINER);
            restored.PasswordHash.Should().Be(original.PasswordHash);
            restored.CreatedAt.Should().Be(original.CreatedAt);
            restored.IsNew.Should().BeFalse();
        }

        [Fact]
        public void RoundTrip_ShouldPreserveEmptyNames()
        {
            // Arrange
            var original = User.Rehydrate(
                Id.New(), Email.From("trainer@weighttracker.test"), PersonName.Empty(), PersonName.Empty(),
                new DateOnly(1985, 12, 9), Role.USER, PasswordHash.From("stored-hash"), CreatedAt);

            // Act
            var restored = UserMapper.MapToDomain(UserMapper.MapToEntity(original));

            // Assert
            restored.Name.Should().Be(PersonName.Empty());
            restored.Surname.Should().Be(PersonName.Empty());
        }

        [Fact]
        public void MapToEntity_ShouldStoreTheNormalizedEmail()
        {
            // Arrange & Act
            var user = User.Rehydrate(
                Id.New(), Email.From("Trainer@WeightTracker.TEST"), PersonName.From("Grace"),
                PersonName.From("Hopper"), new DateOnly(1985, 12, 9), Role.TRAINER,
                PasswordHash.From("stored-hash"), CreatedAt);

            // Assert
            UserMapper.MapToEntity(user).Email.Should().Be("trainer@weighttracker.test");
        }

        [Fact]
        public void MapToEntity_ShouldStampCreatedAtAsUtc()
        {
            // Arrange
            var user = AUser();

            // Act
            var entity = UserMapper.MapToEntity(user);

            // Assert
            entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnUnknownRole()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Role = 99;

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectAMalformedStoredEmail()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Email = "not-an-email";

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRestoreAnEmptyStoredNameAsEmpty()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Name = string.Empty;

            // Act
            var user = UserMapper.MapToDomain(entity);

            // Assert
            user.Name.Should().Be(PersonName.Empty());
        }

        [Fact]
        public void MapToDomain_ShouldRestoreAnEmptyStoredSurnameAsEmpty()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Surname = string.Empty;

            // Act
            var user = UserMapper.MapToDomain(entity);

            // Assert
            user.Surname.Should().Be(PersonName.Empty());
        }

        [Fact]
        public void MapToDomain_ShouldRejectAWhitespaceStoredName()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Name = "   ";

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectAWhitespaceStoredSurname()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Surname = "   ";

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnEmptyStoredPasswordHash()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.PasswordHash = string.Empty;

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnEmptyIdentifier()
        {
            // Arrange
            var entity = UserMapper.MapToEntity(AUser());
            entity.Id = Guid.Empty;

            // Act
            var act = () => UserMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }
    }

    public class Exercises
    {
        private static Exercise AStoredExercise() => Exercise.Rehydrate(
            Id.New(),
            FreeExerciseDbId.From("Plank"),
            Url.From("https://videos.example.com/plancha"),
            Muscles.ABDOMINALS,
            ExerciseTypes.STRENGTH,
            [
                ExerciseTranslation.Rehydrate(Id.New(), LanguageCode.From("es"), ExerciseName.From("Plancha")),
                ExerciseTranslation.Rehydrate(Id.New(), LanguageCode.From("en"), ExerciseName.From("Plank")),
            ]);

        [Fact]
        public void RoundTrip_ShouldPreserveEveryFieldAndEveryTranslation()
        {
            // Arrange
            var original = AStoredExercise();

            // Act
            var restored = ExerciseMapper.MapToDomain(ExerciseMapper.MapToEntity(original));

            // Assert
            restored.Id.Should().Be(original.Id);
            restored.FreeExerciseDbId!.Value.Should().Be("Plank");
            restored.VideoUrl!.Value.Should().Be("https://videos.example.com/plancha");
            restored.Muscle.Should().Be(Muscles.ABDOMINALS);
            restored.BodyPart.Should().Be(BodyParts.CORE);
            restored.Type.Should().Be(ExerciseTypes.STRENGTH);
            restored.IsNew.Should().BeFalse();
            restored.Translations
                .Select(translation => (translation.Id, translation.LanguageCode.Value, translation.Name.Value))
                .Should().BeEquivalentTo(original.Translations
                    .Select(translation => (translation.Id, translation.LanguageCode.Value, translation.Name.Value)));
        }

        [Fact]
        public void MapToEntity_ShouldPointEveryTranslationRowAtItsExercise()
        {
            // Arrange
            var original = AStoredExercise();

            // Act
            var entity = ExerciseMapper.MapToEntity(original);

            // Assert
            entity.Translations.Should().HaveCount(2).And.OnlyContain(row => row.ExerciseId == original.Id.Value);
        }

        [Fact]
        public void MapToDomain_ShouldRestoreTranslationsSortedByLanguageCode()
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());

            // Act
            var restored = ExerciseMapper.MapToDomain(entity);

            // Assert
            restored.Translations.Select(translation => translation.LanguageCode.Value).Should().Equal("en", "es");
        }

        [Fact]
        public void RoundTrip_ShouldPreserveAMissingVideoUrlFreeExerciseDbIdAndTranslations()
        {
            // Arrange
            var original = Exercise.Rehydrate(Id.New(), null, null, Muscles.QUADRICEPS, ExerciseTypes.STRENGTH, []);

            // Act
            var restored = ExerciseMapper.MapToDomain(ExerciseMapper.MapToEntity(original));

            // Assert
            restored.VideoUrl.Should().BeNull();
            restored.FreeExerciseDbId.Should().BeNull();
            restored.Translations.Should().BeEmpty();
        }

        [Theory]
        [InlineData(ExerciseTypes.CARDIO)]
        [InlineData(ExerciseTypes.STRETCHING)]
        public void RoundTrip_ShouldPreserveTheType(ExerciseTypes type)
        {
            // Arrange
            var original = Exercise.Rehydrate(Id.New(), null, null, Muscles.QUADRICEPS, type, []);

            // Act
            var restored = ExerciseMapper.MapToDomain(ExerciseMapper.MapToEntity(original));

            // Assert
            restored.Type.Should().Be(type);
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnUnknownStoredType()
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());
            entity.Type = 99;

            // Act
            var act = () => ExerciseMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Theory]
        [InlineData("https://video", null)]
        [InlineData(null, "   ")]
        public void MapToDomain_ShouldRejectAStoredUrlOrFreeExerciseDbIdThatBreaksTheDomainRules(
            string? videoUrl, string? freeExerciseDbId)
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());
            entity.VideoUrl = videoUrl;
            entity.FreeExerciseDbId = freeExerciseDbId;

            // Act
            var act = () => ExerciseMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Theory]
        [InlineData("spa", "Plancha")]
        [InlineData("es", "ab")]
        public void MapToDomain_ShouldRejectAStoredTranslationThatBreaksTheDomainRules(string languageCode, string name)
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());
            entity.Translations[0].LanguageCode = languageCode;
            entity.Translations[0].Name = name;

            // Act
            var act = () => ExerciseMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectTwoStoredTranslationsForTheSameLanguage()
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());
            entity.Translations[1].LanguageCode = entity.Translations[0].LanguageCode;

            // Act
            var act = () => ExerciseMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnUnknownMuscle()
        {
            // Arrange
            var entity = ExerciseMapper.MapToEntity(AStoredExercise());
            entity.Muscle = 99;

            // Act
            var act = () => ExerciseMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }
    }

    public class RefreshTokens
    {
        private static RefreshToken AToken() =>
            RefreshToken.Create(Id.New(), "token-hash", CreatedAt.AddDays(30), CreatedAt);

        [Fact]
        public void RoundTrip_ShouldPreserveAnActiveToken()
        {
            // Arrange
            var original = AToken();

            // Act
            var restored = RefreshTokenMapper.MapToDomain(RefreshTokenMapper.MapToEntity(original));

            // Assert
            restored.Id.Should().Be(original.Id);
            restored.UserId.Should().Be(original.UserId);
            restored.TokenHash.Should().Be("token-hash");
            restored.ExpiresAt.Should().Be(original.ExpiresAt);
            restored.RevokedAt.Should().BeNull();
            restored.ReplacedByTokenId.Should().BeNull();
            restored.IsNew.Should().BeFalse();
        }

        [Fact]
        public void RoundTrip_ShouldPreserveRevocationAndItsReplacement()
        {
            // Arrange
            var original = AToken();
            var replacement = Id.New();
            original.Revoke(replacement, CreatedAt.AddDays(1));

            // Act
            var restored = RefreshTokenMapper.MapToDomain(RefreshTokenMapper.MapToEntity(original));

            // Assert
            restored.RevokedAt.Should().Be(CreatedAt.AddDays(1));
            restored.ReplacedByTokenId.Should().Be(replacement);
            restored.IsActive(CreatedAt.AddDays(2)).Should().BeFalse();
        }

        [Fact]
        public void MapToEntity_ShouldStampEveryTimestampAsUtc()
        {
            // Arrange
            var original = AToken();
            original.Revoke(Id.New(), CreatedAt.AddDays(1));

            // Act
            var entity = RefreshTokenMapper.MapToEntity(original);

            // Assert
            entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
            entity.ExpiresAt.Kind.Should().Be(DateTimeKind.Utc);
            entity.RevokedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void MapToDomain_ShouldRejectAnEmptyUserIdentifier()
        {
            // Arrange
            var entity = RefreshTokenMapper.MapToEntity(AToken());
            entity.UserId = Guid.Empty;

            // Act
            var act = () => RefreshTokenMapper.MapToDomain(entity);

            // Assert
            act.Should().Throw<PersistenceMappingException>();
        }
    }
}
