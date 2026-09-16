/*
    Initial GottaGo schema.

    Forward-only: once this script has run anywhere, it is never edited. Changes go in a new
    numbered script. DbUp records what it has applied in the SchemaVersions table.
*/

CREATE TABLE dbo.Users
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Email             NVARCHAR(256)    NOT NULL,
    DisplayName       NVARCHAR(100)    NOT NULL,
    -- Null for people who only ever sign in with Google.
    PasswordHash      NVARCHAR(500)    NULL,
    EmailConfirmed    BIT              NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT (0),
    AccessFailedCount INT              NOT NULL CONSTRAINT DF_Users_AccessFailedCount DEFAULT (0),
    LockoutEndUtc     DATETIME2(3)     NULL,
    IsSeedUser        BIT              NOT NULL CONSTRAINT DF_Users_IsSeedUser DEFAULT (0),
    CreatedAtUtc      DATETIME2(3)     NOT NULL
);

CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);

CREATE TABLE dbo.ExternalLogins
(
    Provider     NVARCHAR(50)     NOT NULL,
    ProviderKey  NVARCHAR(200)    NOT NULL,
    UserId       UNIQUEIDENTIFIER NOT NULL,
    CreatedAtUtc DATETIME2(3)     NOT NULL,
    CONSTRAINT PK_ExternalLogins PRIMARY KEY (Provider, ProviderKey),
    CONSTRAINT FK_ExternalLogins_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);

CREATE TABLE dbo.RefreshTokens
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NOT NULL,
    -- SHA-256 of the token. The raw value only ever exists in the user's cookie.
    TokenHash     CHAR(64)         NOT NULL,
    -- Tokens rotated from one another share a family, so reuse can revoke the whole chain.
    FamilyId      UNIQUEIDENTIFIER NOT NULL,
    ExpiresAtUtc  DATETIME2(3)     NOT NULL,
    RevokedAtUtc  DATETIME2(3)     NULL,
    CreatedAtUtc  DATETIME2(3)     NOT NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
CREATE INDEX IX_RefreshTokens_FamilyId ON dbo.RefreshTokens (FamilyId);

CREATE TABLE dbo.Bathrooms
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Bathrooms PRIMARY KEY,
    Slug            NVARCHAR(120)    NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    Description     NVARCHAR(1000)   NULL,
    Street          NVARCHAR(200)    NOT NULL CONSTRAINT DF_Bathrooms_Street DEFAULT (''),
    City            NVARCHAR(100)    NOT NULL,
    State           NVARCHAR(50)     NOT NULL,
    PostalCode      NVARCHAR(20)     NOT NULL CONSTRAINT DF_Bathrooms_PostalCode DEFAULT (''),
    Latitude        FLOAT            NOT NULL,
    Longitude       FLOAT            NOT NULL,
    Venue           INT              NOT NULL CONSTRAINT DF_Bathrooms_Venue DEFAULT (0),
    AccessNote      NVARCHAR(300)    NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    IsSeedData      BIT              NOT NULL CONSTRAINT DF_Bathrooms_IsSeedData DEFAULT (0),
    CreatedAtUtc    DATETIME2(3)     NOT NULL,
    CONSTRAINT FK_Bathrooms_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Bathrooms_Latitude CHECK (Latitude BETWEEN -90 AND 90),
    CONSTRAINT CK_Bathrooms_Longitude CHECK (Longitude BETWEEN -180 AND 180)
);

CREATE UNIQUE INDEX UX_Bathrooms_Slug ON dbo.Bathrooms (Slug);
-- Map queries filter on a viewport, which is a range scan over both coordinates.
CREATE INDEX IX_Bathrooms_Location ON dbo.Bathrooms (Latitude, Longitude) INCLUDE (Name, Slug);

CREATE TABLE dbo.Reviews
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
    BathroomId    UNIQUEIDENTIFIER NOT NULL,
    AuthorId      UNIQUEIDENTIFIER NOT NULL,
    Headline      NVARCHAR(120)    NULL,
    Body          NVARCHAR(2000)   NOT NULL,
    Smell         TINYINT          NOT NULL,
    Cleanliness   TINYINT          NOT NULL,
    Amenities     TINYINT          NOT NULL,
    Accessibility TINYINT          NOT NULL,
    Ambience      TINYINT          NOT NULL,
    VisitedOn     DATE             NULL,
    IsSeedData    BIT              NOT NULL CONSTRAINT DF_Reviews_IsSeedData DEFAULT (0),
    CreatedAtUtc  DATETIME2(3)     NOT NULL,
    CONSTRAINT FK_Reviews_Bathrooms FOREIGN KEY (BathroomId) REFERENCES dbo.Bathrooms (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (AuthorId) REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Reviews_Smell CHECK (Smell BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_Cleanliness CHECK (Cleanliness BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_Amenities CHECK (Amenities BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_Accessibility CHECK (Accessibility BETWEEN 1 AND 5),
    CONSTRAINT CK_Reviews_Ambience CHECK (Ambience BETWEEN 1 AND 5)
);

-- One review per person per bathroom. Re-reviewing edits the existing row, which keeps the
-- averages honest instead of letting one enthusiast vote repeatedly.
CREATE UNIQUE INDEX UX_Reviews_Bathroom_Author ON dbo.Reviews (BathroomId, AuthorId);
CREATE INDEX IX_Reviews_CreatedAtUtc ON dbo.Reviews (CreatedAtUtc DESC);

CREATE TABLE dbo.BathroomPhotos
(
    Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BathroomPhotos PRIMARY KEY,
    BathroomId       UNIQUEIDENTIFIER NOT NULL,
    BlobName         NVARCHAR(300)    NOT NULL,
    -- Not nullable on purpose: an image with no description is unusable to a screen reader.
    AltText          NVARCHAR(300)    NOT NULL,
    SortOrder        INT              NOT NULL CONSTRAINT DF_BathroomPhotos_SortOrder DEFAULT (0),
    UploadedByUserId UNIQUEIDENTIFIER NULL,
    IsSeedData       BIT              NOT NULL CONSTRAINT DF_BathroomPhotos_IsSeedData DEFAULT (0),
    CreatedAtUtc     DATETIME2(3)     NOT NULL,
    CONSTRAINT FK_BathroomPhotos_Bathrooms FOREIGN KEY (BathroomId) REFERENCES dbo.Bathrooms (Id) ON DELETE CASCADE
);

CREATE INDEX IX_BathroomPhotos_BathroomId ON dbo.BathroomPhotos (BathroomId, SortOrder);
