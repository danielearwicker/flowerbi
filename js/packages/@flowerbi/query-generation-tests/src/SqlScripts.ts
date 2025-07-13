export const sqlScripts = {
  SetupTestingDb: `
    CREATE TABLE Testing.Department (
      Id BIGINT NOT NULL,
      DepartmentName NVARCHAR(50) NOT NULL,
      PRIMARY KEY (Id)
    );

    INSERT INTO Testing.Department (Id, DepartmentName) VALUES 
      (1, 'Accounts'),
      (2, 'Marketing'),
      (3, 'Missiles'),
      (4, 'Cheese'),
      (5, 'Yoga'),
      (6, 'Sales');

    CREATE TABLE Testing.Supplier (
      Id BIGINT NOT NULL,
      VendorName NVARCHAR(50) NOT NULL,
      DepartmentId BIGINT NOT NULL,
      PRIMARY KEY(Id)
    );

    INSERT INTO Testing.Supplier (Id, VendorName, DepartmentId) VALUES 
      (1,  'Acme Ltd', 4),
      (2,  'Manchesterford Supplies Inc', 4),
      (3,  'Party Hats 4 U', 2),
      (4,  'United Cheese', 4),
      (5,  'Mats and More', 5),
      (6,  'Uranium 4 Less', 3),
      (7,  'Tiles Tiles Tiles', 2),
      (8,  'Steve Makes Sandwiches', 4),
      (9,  'Handbags-a-Plenty', 3),
      (10, 'Awnings-R-Us', 4),
      (11, 'Disgusting Ltd', 5),
      (12, 'Statues While You Wait', 4),
      (13, 'Stationary Stationery', 1),
      (14, 'Pleasant Plc', 3);

    CREATE TABLE Testing.Invoice (
      Id BIGINT NOT NULL,
      VendorId BIGINT NOT NULL,
      DepartmentId BIGINT NOT NULL,
      FancyAmount DECIMAL(10, 2) NOT NULL,
      Paid BIT NULL,
      PRIMARY KEY(Id)
    );

    INSERT INTO Testing.Invoice (Id, VendorId, DepartmentId, FancyAmount, Paid) VALUES 
      (1,  14, 2, 88.12, NULL),
      (2,  4,  6, 18.12, NULL),
      (3,  7,  5, 68.12, 1),
      (4,  4,  3, 58.12, 1),
      (5,  9,  6, 38.12, NULL),
      (6,  9,  3, 88.12, NULL),
      (7,  8,  1, 88.12, 1),
      (8,  4,  4, 98.12, NULL),
      (9,  3,  2, 58.12, NULL),
      (10, 9,  6, 28.12, NULL),
      (11, 12, 4, 78.12, 0),
      (12, 10, 2, 88.12, 1),
      (13, 5,  6, 58.12, NULL),
      (14, 4,  5, 38.12, 1),
      (15, 11, 1, 68.12, NULL),
      (16, 7,  4, 38.12, NULL),
      (17, 5,  2, 18.12, NULL),
      (18, 8,  5, 88.12, 1),
      (19, 12, 3, 78.12, 1),
      (20, 9,  3, 98.12, NULL),
      (21, 4,  3, 88.12, NULL),
      (22, 2,  5, 58.12, NULL),
      (23, 2,  6, 88.12, 1),
      (24, 4,  5, 38.12, 0),
      (25, 11, 5, 88.02, NULL),
      (26, 13, 2, 28.12, NULL),
      (27, 2,  1, 18.12, 1),
      (28, 6,  1, 88.12, NULL),
      (29, 4,  4, 68.12, NULL);

    CREATE TABLE Testing.Tag (
      Id BIGINT NOT NULL,
      TagName NVARCHAR(50) NOT NULL,
      PRIMARY KEY (Id)
    );

    INSERT INTO Testing.Tag (Id, TagName) VALUES
      (1, 'Interesting'),
      (2, 'Boring'),
      (3, 'Lethal');

    CREATE TABLE Testing.InvoiceTag (
      InvoiceId BIGINT NOT NULL,
      TagId BIGINT NOT NULL,
      PRIMARY KEY(InvoiceId, TagId)
    );

    INSERT INTO Testing.InvoiceTag (InvoiceId, TagId) VALUES
      (4,  1),
      (4,  3),
      (9,  2),
      (10, 2),
      (11, 2),
      (18, 1),
      (20, 1),
      (20, 2),
      (20, 3);

    CREATE TABLE Testing.Category (
      Id BIGINT NOT NULL,
      CategoryName NVARCHAR(50) NOT NULL,
      DepartmentId BIGINT NOT NULL, -- only declared in ComplicatedSchema
      PRIMARY KEY (Id)
    );

    INSERT INTO Testing.Category (Id, CategoryName, DepartmentId) VALUES
      (1, 'Special', 4),
      (2, 'Regular', 4),
      (3, 'Illegal', 3);

    CREATE TABLE Testing.InvoiceCategory (
      InvoiceId BIGINT NOT NULL,
      CategoryId BIGINT NOT NULL,
      PRIMARY KEY(InvoiceId, CategoryId)
    );

    INSERT INTO Testing.InvoiceCategory (InvoiceId, CategoryId) VALUES
      (3,  1),
      (7,  3),
      (8,  2),
      (10, 2),
      (11, 2),
      (11, 3),
      (13, 1),
      (15, 1),
      (24, 2),
      (24, 3);

    CREATE TABLE Testing.AnnotationName (
      Id BIGINT NOT NULL,
      Name NVARCHAR(50) NOT NULL,
      DepartmentId BIGINT NOT NULL, -- only declared in ComplicatedSchema
      PRIMARY KEY (Id)
    );

    INSERT INTO Testing.AnnotationName (Id, Name, DepartmentId) VALUES
      (1, 'Approver', 4),
      (2, 'Instructions', 5),
      (3, 'Movie', 4);

    CREATE TABLE Testing.AnnotationValue (
      Id BIGINT NOT NULL,
      AnnotationNameId BIGINT NOT NULL,
      Value NVARCHAR(50) NOT NULL,
      DepartmentId BIGINT NOT NULL, -- only declared in ComplicatedSchema
      PRIMARY KEY (Id)
    );

    INSERT INTO Testing.AnnotationValue (Id, AnnotationNameId, Value, DepartmentId) VALUES
      (1, 1, 'Jill', 4),
      (2, 1, 'Gupta', 4),
      (3, 1, 'Snarvu', 4),
      (4, 2, 'Pay quickly', 5),
      (5, 2, 'Brown envelope job', 5),
      (6, 2, 'Cash only', 5),
      (7, 2, 'Meet me behind the tree', 5),
      (8, 3, 'Robocop', 4),
      (9, 3, 'Jaws', 4);

    CREATE TABLE Testing.InvoiceAnnotation (
      InvoiceId BIGINT NOT NULL,
      AnnotationValueId BIGINT NOT NULL,
      PRIMARY KEY (InvoiceId, AnnotationValueId)
    );

    INSERT INTO Testing.InvoiceAnnotation(InvoiceId, AnnotationValueId) VALUES
      (1, 1),
      (1, 4),
      (1, 5),
      (1, 8),
      (2, 2),
      (2, 6),
      (2, 9),
      (4, 3),
      (8, 7),
      (8, 9),
      (10, 1),
      (10, 2),
      (10, 3),
      (11, 3),
      (11, 6),
      (11, 8);
  `
};