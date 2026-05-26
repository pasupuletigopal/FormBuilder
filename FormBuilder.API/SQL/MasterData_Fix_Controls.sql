-- ============================================================
-- MasterData_Fix_Controls.sql
-- Run this AFTER MasterData_Seed.sql
--
-- What this does:
--   1. Renames 7 controls whose names don't match the Angular frontend
--   2. Inserts 3 controls that are completely missing (LinkButton, IconButton, Section)
--
-- The Angular frontend matches controls by controlTypeName (the Name column).
-- Source of truth: knownTypes set in control-preview.component.ts
-- ============================================================

SET NOCOUNT ON;

-- ============================================================
-- PART 1 – Fix wrong names  (UPDATE existing rows so FK refs survive)
-- ============================================================

-- TextArea  →  MultiTextBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TextArea')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiTextBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'MultiTextBox',
           [DisplayName] = 'Multi-line Text Box',
           [Icon]        = 'notes',
           [Description] = 'Multi-line text input (textarea)',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'TextArea';
    PRINT 'Renamed TextArea → MultiTextBox';
END
ELSE
    PRINT 'MultiTextBox already exists or TextArea not found – skipped rename';

-- Number  →  NumberBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Number')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'NumberBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'NumberBox',
           [DisplayName] = 'Number Box',
           [Icon]        = 'pin',
           [Description] = 'Numeric input (integer or decimal)',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'Number';
    PRINT 'Renamed Number → NumberBox';
END
ELSE
    PRINT 'NumberBox already exists or Number not found – skipped rename';

-- Email  →  EmailBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Email')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'EmailBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'EmailBox',
           [DisplayName] = 'Email Box',
           [Icon]        = 'email',
           [Description] = 'Email address input with format validation',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'Email';
    PRINT 'Renamed Email → EmailBox';
END
ELSE
    PRINT 'EmailBox already exists or Email not found – skipped rename';

-- Password  →  PasswordBox
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Password')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'PasswordBox')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'PasswordBox',
           [DisplayName] = 'Password Box',
           [Icon]        = 'lock',
           [Description] = 'Masked password input',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'Password';
    PRINT 'Renamed Password → PasswordBox';
END
ELSE
    PRINT 'PasswordBox already exists or Password not found – skipped rename';

-- Dropdown  →  DropdownList
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Dropdown')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DropdownList')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'DropdownList',
           [DisplayName] = 'Dropdown List',
           [Icon]        = 'arrow_drop_down',
           [Description] = 'Single-select dropdown list',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'Dropdown';
    PRINT 'Renamed Dropdown → DropdownList';
END
ELSE
    PRINT 'DropdownList already exists or Dropdown not found – skipped rename';

-- RadioGroup  →  RadioButton
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioGroup')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioButton')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'RadioButton',
           [DisplayName] = 'Radio Button Group',
           [Icon]        = 'radio_button_checked',
           [Description] = 'Single choice from a list of visible options',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'RadioGroup';
    PRINT 'Renamed RadioGroup → RadioButton';
END
ELSE
    PRINT 'RadioButton already exists or RadioGroup not found – skipped rename';

-- Toggle  →  ToggleSwitch
IF EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Toggle')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ToggleSwitch')
BEGIN
    UPDATE [dbo].[ControlTypes]
    SET    [Name]        = 'ToggleSwitch',
           [DisplayName] = 'Toggle Switch',
           [Icon]        = 'toggle_on',
           [Description] = 'On/off toggle switch',
           [UpdatedAt]   = GETUTCDATE()
    WHERE  [Name] = 'Toggle';
    PRINT 'Renamed Toggle → ToggleSwitch';
END
ELSE
    PRINT 'ToggleSwitch already exists or Toggle not found – skipped rename';


-- ============================================================
-- PART 2 – Insert controls that are completely missing
-- ============================================================

-- LinkButton
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'LinkButton')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('LinkButton','Link Button','link','Clickable hyperlink styled as a button','Action',1,285,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted LinkButton';
END
ELSE
    PRINT 'LinkButton already exists – skipped';

-- IconButton
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'IconButton')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('IconButton','Icon Button','add_circle','Circular icon action button','Action',1,295,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted IconButton';
END
ELSE
    PRINT 'IconButton already exists – skipped';

-- Section
IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Section')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Section','Section / Panel','view_agenda','Collapsible section container for grouping controls','Display',1,255,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Section';
END
ELSE
    PRINT 'Section already exists – skipped';


-- ============================================================
-- PART 3 – Also ensure core controls exist if first seed was never run
--           (safe no-ops if they already exist)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TextBox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('TextBox','Text Box','text_fields','Single-line text input','Input',1,10,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted TextBox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiTextBox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('MultiTextBox','Multi-line Text Box','notes','Multi-line text input (textarea)','Input',1,20,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted MultiTextBox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'NumberBox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('NumberBox','Number Box','pin','Numeric input (integer or decimal)','Input',1,30,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted NumberBox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'EmailBox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('EmailBox','Email Box','email','Email address input with format validation','Input',1,40,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted EmailBox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'PasswordBox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('PasswordBox','Password Box','lock','Masked password input','Input',1,50,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted PasswordBox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DatePicker')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('DatePicker','Date Picker','calendar_today','Date selector','DateTime',1,80,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted DatePicker';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'TimePicker')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('TimePicker','Time Picker','access_time','Time selector','DateTime',1,90,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted TimePicker';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DateTimePicker')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('DateTimePicker','Date & Time Picker','event','Combined date and time selector','DateTime',1,100,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted DateTimePicker';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'DropdownList')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('DropdownList','Dropdown List','arrow_drop_down','Single-select dropdown list','Selection',1,120,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted DropdownList';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'MultiSelect')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('MultiSelect','Multi-Select','checklist','Multi-select dropdown list','Selection',1,130,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted MultiSelect';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RadioButton')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('RadioButton','Radio Button Group','radio_button_checked','Single choice from a list of visible options','Selection',1,140,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted RadioButton';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Checkbox')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Checkbox','Checkbox','check_box','Single boolean true/false checkbox','Selection',1,150,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Checkbox';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'CheckboxGroup')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('CheckboxGroup','Checkbox Group','checklist_rtl','Multiple checkboxes from a list','Selection',1,160,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted CheckboxGroup';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ToggleSwitch')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('ToggleSwitch','Toggle Switch','toggle_on','On/off toggle switch','Selection',1,170,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted ToggleSwitch';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'FileUpload')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('FileUpload','File Upload','upload_file','Single file upload','File',1,180,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted FileUpload';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ImageUpload')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('ImageUpload','Image Upload','image','Image file upload with preview','File',1,200,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted ImageUpload';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Slider')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Slider','Slider','tune','Range slider input','Advanced',1,330,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Slider';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'ColorPicker')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('ColorPicker','Color Picker','palette','Hex / RGB color picker','Advanced',1,340,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted ColorPicker';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'RichTextEditor')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('RichTextEditor','Rich Text Editor','format_color_text','WYSIWYG rich text editor','Advanced',1,300,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted RichTextEditor';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Label')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Label','Label','label','Static text label','Display',1,210,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Label';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Heading')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Heading','Heading','title','Section heading (H1-H6)','Display',1,220,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Heading';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Divider')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Divider','Divider','horizontal_rule','Horizontal rule / section separator','Display',1,240,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Divider';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Spacer')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Spacer','Spacer','space_bar','Blank vertical space between controls','Display',1,250,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Spacer';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'HtmlContent')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('HtmlContent','HTML Content','code','Raw HTML block','Display',1,260,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted HtmlContent';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ControlTypes] WHERE [Name] = 'Button')
BEGIN
    INSERT INTO [dbo].[ControlTypes] ([Name],[DisplayName],[Icon],[Description],[Category],[IsActive],[SortOrder],[CreatedAt],[UpdatedAt])
    VALUES ('Button','Button','smart_button','Action button (submit / reset / custom)','Action',1,270,GETUTCDATE(),GETUTCDATE());
    PRINT 'Inserted Button';
END


-- ============================================================
-- Verification – show final list
-- ============================================================
SELECT [Id], [Name], [DisplayName], [Category], [IsActive], [SortOrder]
FROM   [dbo].[ControlTypes]
ORDER  BY [Category], [SortOrder];

SET NOCOUNT OFF;
