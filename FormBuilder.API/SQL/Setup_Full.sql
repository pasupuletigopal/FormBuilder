-- ============================================================
-- Setup_Full.sql
-- FormBuilder – complete database setup in a single script.
-- Safe to run on a fresh OR an existing database (fully idempotent).
--
-- Execution order:
--   STEP 1  Create ApiManager tables     (from ApiManager_CreateTables.sql)
--   STEP 2  Seed master data             (from MasterData_Seed.sql)
--   STEP 3  Fix control names            (from MasterData_Fix_Controls.sql)
-- ============================================================

SET NOCOUNT ON;

PRINT '=== FormBuilder Full Setup ===';
PRINT '';

-- ============================================================
-- STEP 1 – ApiManager tables
-- ============================================================
PRINT '-- STEP 1: ApiManager tables --';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiEnvironments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiEnvironments] (
        [Id]        INT           IDENTITY(1,1) PRIMARY KEY,
        [Name]      NVARCHAR(100) NOT NULL,
        [Variables] NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        [SortOrder] INT           NOT NULL DEFAULT 0,
        [IsActive]  BIT           NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Created ApiEnvironments';
END
ELSE
    PRINT 'ApiEnvironments already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiCollections' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiCollections] (
        [Id]          INT           IDENTITY(1,1) PRIMARY KEY,
        [Name]        NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [SortOrder]   INT           NOT NULL DEFAULT 0,
        [IsActive]    BIT           NOT NULL DEFAULT 1,
        [CreatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Created ApiCollections';
END
ELSE
    PRINT 'ApiCollections already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiFolders' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiFolders] (
        [Id]             INT           IDENTITY(1,1) PRIMARY KEY,
        [CollectionId]   INT           NOT NULL,
        [ParentFolderId] INT           NULL,
        [Name]           NVARCHAR(200) NOT NULL,
        [SortOrder]      INT           NOT NULL DEFAULT 0,
        [CreatedAt]      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_ApiFolders_ApiCollections] FOREIGN KEY ([CollectionId])   REFERENCES [dbo].[ApiCollections]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApiFolders_ParentFolder]   FOREIGN KEY ([ParentFolderId]) REFERENCES [dbo].[ApiFolders]([Id])
    );
    PRINT 'Created ApiFolders';
END
ELSE
    PRINT 'ApiFolders already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiRequests] (
        [Id]           INT           IDENTITY(1,1) PRIMARY KEY,
        [CollectionId] INT           NOT NULL,
        [FolderId]     INT           NULL,
        [Name]         NVARCHAR(300) NOT NULL,
        [Method]       NVARCHAR(10)  NOT NULL DEFAULT 'GET',
        [Url]          NVARCHAR(MAX) NOT NULL,
        [Headers]      NVARCHAR(MAX) NULL,
        [QueryParams]  NVARCHAR(MAX) NULL,
        [AuthType]     NVARCHAR(50)  NULL,
        [AuthConfig]   NVARCHAR(MAX) NULL,
        [Body]         NVARCHAR(MAX) NULL,
        [BodyType]     NVARCHAR(20)  NULL DEFAULT 'json',
        [SortOrder]    INT           NOT NULL DEFAULT 0,
        [CreatedAt]    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_ApiRequests_ApiCollections] FOREIGN KEY ([CollectionId]) REFERENCES [dbo].[ApiCollections]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApiRequests_ApiFolders]     FOREIGN KEY ([FolderId])     REFERENCES [dbo].[ApiFolders]([Id])
    );
    PRINT 'Created ApiRequests';
END
ELSE
    PRINT 'ApiRequests already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiRequestHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiRequestHistory] (
        [Id]                INT           IDENTITY(1,1) PRIMARY KEY,
        [RequestId]         INT           NOT NULL,
        [StatusCode]        INT           NOT NULL,
        [ResponseHeaders]   NVARCHAR(MAX) NULL,
        [ResponseBody]      NVARCHAR(MAX) NULL,
        [ResponseSizeBytes] BIGINT        NOT NULL DEFAULT 0,
        [DurationMs]        INT           NOT NULL DEFAULT 0,
        [ExecutedAt]        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_ApiRequestHistory_ApiRequests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[ApiRequests]([Id]) ON DELETE CASCADE
    );
    PRINT 'Created ApiRequestHistory';
END
ELSE
    PRINT 'ApiRequestHistory already exists – skipped';

PRINT '';


-- ============================================================
-- STEP 2 – Master data seed
-- ============================================================
PRINT '-- STEP 2: Master data seed --';

-- ControlTypes
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes])
BEGIN
    SET IDENTITY_INSERT [dbo].[ControlTypes] ON;

    INSERT INTO [dbo].[ControlTypes] ([Id],[Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES
    -- Input controls
    ( 1, 'TextBox',       'Text Box',          'text_fields',     'Single-line text input',                    'Input',     1,  10, GETUTCDATE(), GETUTCDATE()),
    ( 2, 'TextArea',      'Text Area',         'notes',           'Multi-line text input',                     'Input',     1,  20, GETUTCDATE(), GETUTCDATE()),
    ( 3, 'Number',        'Number',            'pin',             'Numeric input (integer or decimal)',        'Input',     1,  30, GETUTCDATE(), GETUTCDATE()),
    ( 4, 'Email',         'Email',             'email',           'Email address input with format check',    'Input',     1,  40, GETUTCDATE(), GETUTCDATE()),
    ( 5, 'Phone',         'Phone',             'phone',           'Phone number input',                       'Input',     1,  50, GETUTCDATE(), GETUTCDATE()),
    ( 6, 'Password',      'Password',          'lock',            'Masked password input',                    'Input',     1,  60, GETUTCDATE(), GETUTCDATE()),
    ( 7, 'Url',           'URL',               'link',            'Web address / URL input',                  'Input',     1,  70, GETUTCDATE(), GETUTCDATE()),

    -- Date / Time controls
    ( 8, 'DatePicker',    'Date Picker',       'calendar_today', 'Date selector',                             'DateTime',  1,  80, GETUTCDATE(), GETUTCDATE()),
    ( 9, 'TimePicker',    'Time Picker',       'access_time',    'Time selector',                             'DateTime',  1,  90, GETUTCDATE(), GETUTCDATE()),
    (10, 'DateTimePicker','Date & Time Picker','event',          'Combined date and time selector',           'DateTime',  1, 100, GETUTCDATE(), GETUTCDATE()),
    (11, 'DateRange',     'Date Range',        'date_range',     'Start and end date selector',               'DateTime',  1, 110, GETUTCDATE(), GETUTCDATE()),

    -- Selection controls
    (12, 'Dropdown',      'Dropdown',          'arrow_drop_down','Single-select dropdown list',               'Selection', 1, 120, GETUTCDATE(), GETUTCDATE()),
    (13, 'MultiSelect',   'Multi-Select',      'checklist',      'Multi-select dropdown list',               'Selection', 1, 130, GETUTCDATE(), GETUTCDATE()),
    (14, 'RadioGroup',    'Radio Group',       'radio_button_checked','Single choice from visible options',  'Selection', 1, 140, GETUTCDATE(), GETUTCDATE()),
    (15, 'Checkbox',      'Checkbox',          'check_box',      'Boolean true/false toggle',                'Selection', 1, 150, GETUTCDATE(), GETUTCDATE()),
    (16, 'CheckboxGroup', 'Checkbox Group',    'checklist_rtl',  'Multiple checkboxes from a list',          'Selection', 1, 160, GETUTCDATE(), GETUTCDATE()),
    (17, 'Toggle',        'Toggle Switch',     'toggle_on',      'On/off toggle switch',                     'Selection', 1, 170, GETUTCDATE(), GETUTCDATE()),

    -- File controls
    (18, 'FileUpload',    'File Upload',       'upload_file',    'Single file upload',                       'File',      1, 180, GETUTCDATE(), GETUTCDATE()),
    (19, 'MultiFileUpload','Multi File Upload','drive_folder_upload','Multiple file upload',                 'File',      1, 190, GETUTCDATE(), GETUTCDATE()),
    (20, 'ImageUpload',   'Image Upload',      'image',          'Image file upload with preview',           'File',      1, 200, GETUTCDATE(), GETUTCDATE()),

    -- Layout / display controls
    (21, 'Label',         'Label',             'label',          'Static text label',                        'Display',   1, 210, GETUTCDATE(), GETUTCDATE()),
    (22, 'Heading',       'Heading',           'title',          'Section heading (H1-H6)',                  'Display',   1, 220, GETUTCDATE(), GETUTCDATE()),
    (23, 'Paragraph',     'Paragraph',         'subject',        'Static paragraph text / HTML',             'Display',   1, 230, GETUTCDATE(), GETUTCDATE()),
    (24, 'Divider',       'Divider',           'horizontal_rule','Horizontal rule / section separator',     'Display',   1, 240, GETUTCDATE(), GETUTCDATE()),
    (25, 'Spacer',        'Spacer',            'space_bar',      'Blank space between controls',             'Display',   1, 250, GETUTCDATE(), GETUTCDATE()),
    (26, 'HtmlContent',   'HTML Content',      'code',           'Raw HTML block',                           'Display',   1, 260, GETUTCDATE(), GETUTCDATE()),

    -- Action controls
    (27, 'Button',        'Button',            'smart_button',   'Action button (submit / reset / custom)',  'Action',    1, 270, GETUTCDATE(), GETUTCDATE()),
    (28, 'SubmitButton',  'Submit Button',     'send',           'Form submission button',                   'Action',    1, 280, GETUTCDATE(), GETUTCDATE()),
    (29, 'ResetButton',   'Reset Button',      'restart_alt',    'Clear / reset form button',                'Action',    1, 290, GETUTCDATE(), GETUTCDATE()),

    -- Advanced controls
    (30, 'RichTextEditor','Rich Text Editor',  'format_color_text','WYSIWYG rich text editor',              'Advanced',  1, 300, GETUTCDATE(), GETUTCDATE()),
    (31, 'Signature',     'Signature Pad',     'draw',           'Freehand signature capture',              'Advanced',  1, 310, GETUTCDATE(), GETUTCDATE()),
    (32, 'Rating',        'Rating',            'star',           'Star / numeric rating input',              'Advanced',  1, 320, GETUTCDATE(), GETUTCDATE()),
    (33, 'Slider',        'Slider',            'tune',           'Range slider input',                       'Advanced',  1, 330, GETUTCDATE(), GETUTCDATE()),
    (34, 'ColorPicker',   'Color Picker',      'palette',        'Hex / RGB color picker',                   'Advanced',  1, 340, GETUTCDATE(), GETUTCDATE()),
    (35, 'Hidden',        'Hidden Field',      'visibility_off', 'Hidden field (carries value, no UI)',      'Advanced',  1, 350, GETUTCDATE(), GETUTCDATE());

    SET IDENTITY_INSERT [dbo].[ControlTypes] OFF;
    PRINT 'ControlTypes seeded (35 rows)';
END
ELSE
    PRINT 'ControlTypes already has data – skipped';


-- DataTypes
IF NOT EXISTS (SELECT 1 FROM [dbo].[DataTypes])
BEGIN
    SET IDENTITY_INSERT [dbo].[DataTypes] ON;

    INSERT INTO [dbo].[DataTypes] ([Id],[Name],[DisplayName],[DotNetType],[IsCollection],[IsActive],[CreatedAt]) VALUES
    ( 1, 'string',       'Text',               'System.String',                                                      0, 1, GETUTCDATE()),
    ( 2, 'int',          'Integer',            'System.Int32',                                                       0, 1, GETUTCDATE()),
    ( 3, 'long',         'Long Integer',       'System.Int64',                                                       0, 1, GETUTCDATE()),
    ( 4, 'decimal',      'Decimal',            'System.Decimal',                                                     0, 1, GETUTCDATE()),
    ( 5, 'double',       'Double',             'System.Double',                                                      0, 1, GETUTCDATE()),
    ( 6, 'bool',         'Boolean',            'System.Boolean',                                                     0, 1, GETUTCDATE()),
    ( 7, 'DateTime',     'Date & Time',        'System.DateTime',                                                    0, 1, GETUTCDATE()),
    ( 8, 'DateOnly',     'Date',               'System.DateOnly',                                                    0, 1, GETUTCDATE()),
    ( 9, 'TimeOnly',     'Time',               'System.TimeOnly',                                                    0, 1, GETUTCDATE()),
    (10, 'Guid',         'GUID',               'System.Guid',                                                        0, 1, GETUTCDATE()),
    (11, 'string[]',     'Text List',          'System.String[]',                                                    1, 1, GETUTCDATE()),
    (12, 'int[]',        'Integer List',       'System.Int32[]',                                                     1, 1, GETUTCDATE()),
    (13, 'List<string>', 'Text Collection',    'System.Collections.Generic.List`1[[System.String]]',                 1, 1, GETUTCDATE()),
    (14, 'List<int>',    'Integer Collection', 'System.Collections.Generic.List`1[[System.Int32]]',                  1, 1, GETUTCDATE()),
    (15, 'object',       'Object (JSON)',       'System.Object',                                                      0, 1, GETUTCDATE()),
    (16, 'byte[]',       'Binary / File',      'System.Byte[]',                                                      0, 1, GETUTCDATE());

    SET IDENTITY_INSERT [dbo].[DataTypes] OFF;
    PRINT 'DataTypes seeded (16 rows)';
END
ELSE
    PRINT 'DataTypes already has data – skipped';


-- ValidationRules
IF NOT EXISTS (SELECT 1 FROM [dbo].[ValidationRules])
BEGIN
    SET IDENTITY_INSERT [dbo].[ValidationRules] ON;

    INSERT INTO [dbo].[ValidationRules] ([Id],[Name],[DisplayName],[Pattern],[ErrorMessage],[IsActive],[CreatedAt]) VALUES
    -- Core
    ( 1, 'Required',           'Required',              NULL,
        'This field is required.',                                                                          1, GETUTCDATE()),
    ( 2, 'MinLength',          'Minimum Length',        NULL,
        'Must be at least {min} characters.',                                                               1, GETUTCDATE()),
    ( 3, 'MaxLength',          'Maximum Length',        NULL,
        'Must be no more than {max} characters.',                                                           1, GETUTCDATE()),
    ( 4, 'MinValue',           'Minimum Value',         NULL,
        'Must be greater than or equal to {min}.',                                                          1, GETUTCDATE()),
    ( 5, 'MaxValue',           'Maximum Value',         NULL,
        'Must be less than or equal to {max}.',                                                             1, GETUTCDATE()),
    ( 6, 'RangeLength',        'Length Range',          NULL,
        'Must be between {min} and {max} characters.',                                                      1, GETUTCDATE()),
    ( 7, 'Range',              'Value Range',           NULL,
        'Must be between {min} and {max}.',                                                                 1, GETUTCDATE()),

    -- Format patterns
    ( 8, 'Email',              'Email Address',
        '^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$',
        'Please enter a valid email address.',                                                              1, GETUTCDATE()),
    ( 9, 'PhoneUS',            'US Phone Number',
        '^(\+1\s?)?((\([0-9]{3}\))|[0-9]{3})[\s\-]?[0-9]{3}[\s\-]?[0-9]{4}$',
        'Please enter a valid US phone number.',                                                            1, GETUTCDATE()),
    (10, 'PhoneInternational', 'International Phone',
        '^\+?[1-9]\d{6,14}$',
        'Please enter a valid international phone number.',                                                 1, GETUTCDATE()),
    (11, 'Url',                'URL',
        '^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$',
        'Please enter a valid URL (https://...).',                                                          1, GETUTCDATE()),
    (12, 'AlphaOnly',          'Letters Only',
        '^[a-zA-Z]+$',
        'Only alphabetic characters are allowed.',                                                          1, GETUTCDATE()),
    (13, 'AlphaNumeric',       'Alphanumeric',
        '^[a-zA-Z0-9]+$',
        'Only letters and numbers are allowed.',                                                            1, GETUTCDATE()),
    (14, 'NumericOnly',        'Numbers Only',
        '^[0-9]+$',
        'Only numeric digits are allowed.',                                                                 1, GETUTCDATE()),
    (15, 'NoSpecialChars',     'No Special Characters',
        '^[a-zA-Z0-9 ]+$',
        'Special characters are not allowed.',                                                              1, GETUTCDATE()),
    (16, 'ZipCodeUS',          'US ZIP Code',
        '^\d{5}(-\d{4})?$',
        'Please enter a valid US ZIP code (e.g. 12345 or 12345-6789).',                                   1, GETUTCDATE()),
    (17, 'PostalCodeCA',       'Canadian Postal Code',
        '^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$',
        'Please enter a valid Canadian postal code (e.g. A1B 2C3).',                                      1, GETUTCDATE()),
    (18, 'SSN',                'Social Security Number',
        '^\d{3}-\d{2}-\d{4}$',
        'Please enter a valid SSN (e.g. 123-45-6789).',                                                   1, GETUTCDATE()),
    (19, 'CreditCard',         'Credit Card Number',
        '^(?:4[0-9]{12}(?:[0-9]{3})?|[25][1-7][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11})$',
        'Please enter a valid credit card number.',                                                         1, GETUTCDATE()),
    (20, 'IPv4',               'IPv4 Address',
        '^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$',
        'Please enter a valid IPv4 address.',                                                               1, GETUTCDATE()),
    (21, 'Date',               'Date (YYYY-MM-DD)',
        '^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$',
        'Please enter a valid date in YYYY-MM-DD format.',                                                  1, GETUTCDATE()),
    (22, 'Time24h',            'Time 24-Hour',
        '^([01][0-9]|2[0-3]):[0-5][0-9]$',
        'Please enter a valid 24-hour time (HH:MM).',                                                       1, GETUTCDATE()),
    (23, 'CustomRegex',        'Custom Regex Pattern',  NULL,
        'The value does not match the required format.',                                                    1, GETUTCDATE());

    SET IDENTITY_INSERT [dbo].[ValidationRules] OFF;
    PRINT 'ValidationRules seeded (23 rows)';
END
ELSE
    PRINT 'ValidationRules already has data – skipped';


-- DataSources
IF NOT EXISTS (SELECT 1 FROM [dbo].[DataSources])
BEGIN
    SET IDENTITY_INSERT [dbo].[DataSources] ON;

    INSERT INTO [dbo].[DataSources] ([Id],[Name],[Description],[SourceType],[ValueField],[LabelField],[IsActive],[CreatedAt],[UpdatedAt]) VALUES
    ( 1, 'Yes / No',    'Simple yes/no boolean list',       'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 2, 'Gender',      'Gender options',                   'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 3, 'Salutation',  'Name prefixes / titles',           'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 4, 'Days of Week','Monday through Sunday',            'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 5, 'Months',      'January through December',        'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 6, 'US States',   '50 US states plus DC',             'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 7, 'Priority',    'Low / Medium / High / Critical',   'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 8, 'Status',      'Active / Inactive / Pending',      'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    ( 9, 'Rating 1-5',  'Star rating 1 through 5',          'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE()),
    (10, 'Age Range',   'Common age brackets',              'Static', 'value', 'label', 1, GETUTCDATE(), GETUTCDATE());

    SET IDENTITY_INSERT [dbo].[DataSources] OFF;
    PRINT 'DataSources seeded (10 rows)';
END
ELSE
    PRINT 'DataSources already has data – skipped';


-- DataSourceItems
IF NOT EXISTS (SELECT 1 FROM [dbo].[DataSourceItems])
BEGIN
    -- Yes / No (1)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (1,'true','Yes',1,0,1,GETUTCDATE()),(1,'false','No',2,0,1,GETUTCDATE());

    -- Gender (2)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (2,'M','Male',1,0,1,GETUTCDATE()),(2,'F','Female',2,0,1,GETUTCDATE()),
    (2,'NB','Non-Binary',3,0,1,GETUTCDATE()),(2,'P','Prefer not to say',4,0,1,GETUTCDATE());

    -- Salutation (3)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (3,'Mr','Mr.',1,0,1,GETUTCDATE()),(3,'Mrs','Mrs.',2,0,1,GETUTCDATE()),
    (3,'Ms','Ms.',3,0,1,GETUTCDATE()),(3,'Dr','Dr.',4,0,1,GETUTCDATE()),
    (3,'Prof','Prof.',5,0,1,GETUTCDATE()),(3,'Rev','Rev.',6,0,1,GETUTCDATE());

    -- Days of Week (4)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (4,'1','Monday',1,0,1,GETUTCDATE()),(4,'2','Tuesday',2,0,1,GETUTCDATE()),
    (4,'3','Wednesday',3,0,1,GETUTCDATE()),(4,'4','Thursday',4,0,1,GETUTCDATE()),
    (4,'5','Friday',5,0,1,GETUTCDATE()),(4,'6','Saturday',6,0,1,GETUTCDATE()),
    (4,'7','Sunday',7,0,1,GETUTCDATE());

    -- Months (5)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (5,'1','January',1,0,1,GETUTCDATE()),(5,'2','February',2,0,1,GETUTCDATE()),
    (5,'3','March',3,0,1,GETUTCDATE()),(5,'4','April',4,0,1,GETUTCDATE()),
    (5,'5','May',5,0,1,GETUTCDATE()),(5,'6','June',6,0,1,GETUTCDATE()),
    (5,'7','July',7,0,1,GETUTCDATE()),(5,'8','August',8,0,1,GETUTCDATE()),
    (5,'9','September',9,0,1,GETUTCDATE()),(5,'10','October',10,0,1,GETUTCDATE()),
    (5,'11','November',11,0,1,GETUTCDATE()),(5,'12','December',12,0,1,GETUTCDATE());

    -- US States (6)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (6,'AL','Alabama',1,0,1,GETUTCDATE()),(6,'AK','Alaska',2,0,1,GETUTCDATE()),
    (6,'AZ','Arizona',3,0,1,GETUTCDATE()),(6,'AR','Arkansas',4,0,1,GETUTCDATE()),
    (6,'CA','California',5,0,1,GETUTCDATE()),(6,'CO','Colorado',6,0,1,GETUTCDATE()),
    (6,'CT','Connecticut',7,0,1,GETUTCDATE()),(6,'DE','Delaware',8,0,1,GETUTCDATE()),
    (6,'DC','District of Columbia',9,0,1,GETUTCDATE()),(6,'FL','Florida',10,0,1,GETUTCDATE()),
    (6,'GA','Georgia',11,0,1,GETUTCDATE()),(6,'HI','Hawaii',12,0,1,GETUTCDATE()),
    (6,'ID','Idaho',13,0,1,GETUTCDATE()),(6,'IL','Illinois',14,0,1,GETUTCDATE()),
    (6,'IN','Indiana',15,0,1,GETUTCDATE()),(6,'IA','Iowa',16,0,1,GETUTCDATE()),
    (6,'KS','Kansas',17,0,1,GETUTCDATE()),(6,'KY','Kentucky',18,0,1,GETUTCDATE()),
    (6,'LA','Louisiana',19,0,1,GETUTCDATE()),(6,'ME','Maine',20,0,1,GETUTCDATE()),
    (6,'MD','Maryland',21,0,1,GETUTCDATE()),(6,'MA','Massachusetts',22,0,1,GETUTCDATE()),
    (6,'MI','Michigan',23,0,1,GETUTCDATE()),(6,'MN','Minnesota',24,0,1,GETUTCDATE()),
    (6,'MS','Mississippi',25,0,1,GETUTCDATE()),(6,'MO','Missouri',26,0,1,GETUTCDATE()),
    (6,'MT','Montana',27,0,1,GETUTCDATE()),(6,'NE','Nebraska',28,0,1,GETUTCDATE()),
    (6,'NV','Nevada',29,0,1,GETUTCDATE()),(6,'NH','New Hampshire',30,0,1,GETUTCDATE()),
    (6,'NJ','New Jersey',31,0,1,GETUTCDATE()),(6,'NM','New Mexico',32,0,1,GETUTCDATE()),
    (6,'NY','New York',33,0,1,GETUTCDATE()),(6,'NC','North Carolina',34,0,1,GETUTCDATE()),
    (6,'ND','North Dakota',35,0,1,GETUTCDATE()),(6,'OH','Ohio',36,0,1,GETUTCDATE()),
    (6,'OK','Oklahoma',37,0,1,GETUTCDATE()),(6,'OR','Oregon',38,0,1,GETUTCDATE()),
    (6,'PA','Pennsylvania',39,0,1,GETUTCDATE()),(6,'RI','Rhode Island',40,0,1,GETUTCDATE()),
    (6,'SC','South Carolina',41,0,1,GETUTCDATE()),(6,'SD','South Dakota',42,0,1,GETUTCDATE()),
    (6,'TN','Tennessee',43,0,1,GETUTCDATE()),(6,'TX','Texas',44,0,1,GETUTCDATE()),
    (6,'UT','Utah',45,0,1,GETUTCDATE()),(6,'VT','Vermont',46,0,1,GETUTCDATE()),
    (6,'VA','Virginia',47,0,1,GETUTCDATE()),(6,'WA','Washington',48,0,1,GETUTCDATE()),
    (6,'WV','West Virginia',49,0,1,GETUTCDATE()),(6,'WI','Wisconsin',50,0,1,GETUTCDATE()),
    (6,'WY','Wyoming',51,0,1,GETUTCDATE());

    -- Priority (7)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (7,'low','Low',1,0,1,GETUTCDATE()),(7,'medium','Medium',2,1,1,GETUTCDATE()),
    (7,'high','High',3,0,1,GETUTCDATE()),(7,'critical','Critical',4,0,1,GETUTCDATE());

    -- Status (8)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (8,'active','Active',1,1,1,GETUTCDATE()),(8,'inactive','Inactive',2,0,1,GETUTCDATE()),
    (8,'pending','Pending',3,0,1,GETUTCDATE()),(8,'archived','Archived',4,0,1,GETUTCDATE());

    -- Rating 1-5 (9)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (9,'1','1 - Poor',1,0,1,GETUTCDATE()),(9,'2','2 - Fair',2,0,1,GETUTCDATE()),
    (9,'3','3 - Good',3,0,1,GETUTCDATE()),(9,'4','4 - Very Good',4,0,1,GETUTCDATE()),
    (9,'5','5 - Excellent',5,0,1,GETUTCDATE());

    -- Age Range (10)
    INSERT INTO [dbo].[DataSourceItems] ([DataSourceId],[Value],[Label],[SortOrder],[IsDefault],[IsActive],[CreatedAt]) VALUES
    (10,'under18','Under 18',1,0,1,GETUTCDATE()),(10,'18-24','18 - 24',2,0,1,GETUTCDATE()),
    (10,'25-34','25 - 34',3,0,1,GETUTCDATE()),(10,'35-44','35 - 44',4,0,1,GETUTCDATE()),
    (10,'45-54','45 - 54',5,0,1,GETUTCDATE()),(10,'55-64','55 - 64',6,0,1,GETUTCDATE()),
    (10,'65plus','65 and over',7,0,1,GETUTCDATE());

    PRINT 'DataSourceItems seeded';
END
ELSE
    PRINT 'DataSourceItems already has data – skipped';

PRINT '';


-- ============================================================
-- STEP 3 – Fix control names to match Angular frontend
-- ============================================================
PRINT '-- STEP 3: Fix control names --';

-- TextArea  →  MultiTextBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TextArea')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiTextBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'MultiTextBox', [DisplayName] = 'Multi-line Text Box',
           [Icon] = 'notes', [Description] = 'Multi-line text input (textarea)', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'TextArea';
    PRINT 'Renamed TextArea -> MultiTextBox';
END
ELSE PRINT 'MultiTextBox already correct or TextArea not found – skipped';

-- Number  →  NumberBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Number')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'NumberBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'NumberBox', [DisplayName] = 'Number Box',
           [Icon] = 'pin', [Description] = 'Numeric input (integer or decimal)', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'Number';
    PRINT 'Renamed Number -> NumberBox';
END
ELSE PRINT 'NumberBox already correct or Number not found – skipped';

-- Email  →  EmailBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Email')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'EmailBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'EmailBox', [DisplayName] = 'Email Box',
           [Icon] = 'email', [Description] = 'Email address input with format validation', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'Email';
    PRINT 'Renamed Email -> EmailBox';
END
ELSE PRINT 'EmailBox already correct or Email not found – skipped';

-- Password  →  PasswordBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Password')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'PasswordBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'PasswordBox', [DisplayName] = 'Password Box',
           [Icon] = 'lock', [Description] = 'Masked password input', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'Password';
    PRINT 'Renamed Password -> PasswordBox';
END
ELSE PRINT 'PasswordBox already correct or Password not found – skipped';

-- Dropdown  →  DropdownList
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Dropdown')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DropdownList')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'DropdownList', [DisplayName] = 'Dropdown List',
           [Icon] = 'arrow_drop_down', [Description] = 'Single-select dropdown list', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'Dropdown';
    PRINT 'Renamed Dropdown -> DropdownList';
END
ELSE PRINT 'DropdownList already correct or Dropdown not found – skipped';

-- RadioGroup  →  RadioButton
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioGroup')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioButton')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'RadioButton', [DisplayName] = 'Radio Button Group',
           [Icon] = 'radio_button_checked', [Description] = 'Single choice from a list of visible options', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'RadioGroup';
    PRINT 'Renamed RadioGroup -> RadioButton';
END
ELSE PRINT 'RadioButton already correct or RadioGroup not found – skipped';

-- Toggle  →  ToggleSwitch
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Toggle')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ToggleSwitch')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name] = 'ToggleSwitch', [DisplayName] = 'Toggle Switch',
           [Icon] = 'toggle_on', [Description] = 'On/off toggle switch', [UpdatedAt] = GETUTCDATE()
    WHERE  [Name] = 'Toggle';
    PRINT 'Renamed Toggle -> ToggleSwitch';
END
ELSE PRINT 'ToggleSwitch already correct or Toggle not found – skipped';

-- Insert missing controls (LinkButton, IconButton, Section)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'LinkButton')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('LinkButton','Link Button','link','Clickable hyperlink styled as a button','Action',1,285,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted LinkButton';
END
ELSE PRINT 'LinkButton already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'IconButton')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('IconButton','Icon Button','add_circle','Circular icon action button','Action',1,295,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted IconButton';
END
ELSE PRINT 'IconButton already exists – skipped';

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Section')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Section','Section / Panel','view_agenda','Collapsible section container for grouping controls','Display',1,255,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Section';
END
ELSE PRINT 'Section already exists – skipped';

-- Safety-net inserts: ensure all Angular-required controls exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TextBox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('TextBox','Text Box','text_fields','Single-line text input','Input',1,10,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiTextBox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('MultiTextBox','Multi-line Text Box','notes','Multi-line text input (textarea)','Input',1,20,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'NumberBox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('NumberBox','Number Box','pin','Numeric input (integer or decimal)','Input',1,30,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'EmailBox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('EmailBox','Email Box','email','Email address input with format validation','Input',1,40,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'PasswordBox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('PasswordBox','Password Box','lock','Masked password input','Input',1,50,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DatePicker')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('DatePicker','Date Picker','calendar_today','Date selector','DateTime',1,80,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TimePicker')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('TimePicker','Time Picker','access_time','Time selector','DateTime',1,90,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DateTimePicker')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('DateTimePicker','Date & Time Picker','event','Combined date and time selector','DateTime',1,100,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DropdownList')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('DropdownList','Dropdown List','arrow_drop_down','Single-select dropdown list','Selection',1,120,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiSelect')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('MultiSelect','Multi-Select','checklist','Multi-select dropdown list','Selection',1,130,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioButton')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('RadioButton','Radio Button Group','radio_button_checked','Single choice from a list of visible options','Selection',1,140,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Checkbox')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Checkbox','Checkbox','check_box','Single boolean true/false checkbox','Selection',1,150,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'CheckboxGroup')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('CheckboxGroup','Checkbox Group','checklist_rtl','Multiple checkboxes from a list','Selection',1,160,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ToggleSwitch')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('ToggleSwitch','Toggle Switch','toggle_on','On/off toggle switch','Selection',1,170,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'FileUpload')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('FileUpload','File Upload','upload_file','Single file upload','File',1,180,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ImageUpload')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('ImageUpload','Image Upload','image','Image file upload with preview','File',1,200,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Slider')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Slider','Slider','tune','Range slider input','Advanced',1,330,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ColorPicker')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('ColorPicker','Color Picker','palette','Hex / RGB color picker','Advanced',1,340,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RichTextEditor')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('RichTextEditor','Rich Text Editor','format_color_text','WYSIWYG rich text editor','Advanced',1,300,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Label')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Label','Label','label','Static text label','Display',1,210,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Heading')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Heading','Heading','title','Section heading (H1-H6)','Display',1,220,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Divider')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Divider','Divider','horizontal_rule','Horizontal rule / section separator','Display',1,240,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Spacer')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Spacer','Spacer','space_bar','Blank vertical space between controls','Display',1,250,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'HtmlContent')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('HtmlContent','HTML Content','code','Raw HTML block','Display',1,260,GETUTCDATE(),GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Button')
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt]) VALUES ('Button','Button','smart_button','Action button (submit / reset / custom)','Action',1,270,GETUTCDATE(),GETUTCDATE());

PRINT '';
PRINT '=== Setup complete ===';
PRINT '';

-- Final verification
SELECT [Id], [Name], [DisplayName], [Category], [IsActive], [SortOrder]
FROM   [dbo].[ControlTypes]
ORDER  BY [Category], [SortOrder];

SET NOCOUNT OFF;
