CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Categories" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Type" INTEGER NOT NULL
);

CREATE TABLE "CreditCardAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CreditCardAccounts" PRIMARY KEY AUTOINCREMENT,
    "BankName" TEXT NOT NULL,
    "CardLabel" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);

CREATE TABLE "InflationCaches" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InflationCaches" PRIMARY KEY AUTOINCREMENT,
    "Year" INTEGER NOT NULL,
    "Month" INTEGER NOT NULL,
    "CpiRate" REAL NOT NULL,
    "FetchedAt" TEXT NOT NULL
);

CREATE TABLE "BudgetLimits" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_BudgetLimits" PRIMARY KEY AUTOINCREMENT,
    "CategoryId" INTEGER NOT NULL,
    "MonthlyLimit" TEXT NOT NULL,
    "BudgetStartDay" INTEGER NOT NULL,
    CONSTRAINT "FK_BudgetLimits_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Transactions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Transactions" PRIMARY KEY AUTOINCREMENT,
    "Amount" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "Description" TEXT NULL,
    "CategoryId" INTEGER NOT NULL,
    "CreditCardAccountId" INTEGER NULL,
    CONSTRAINT "FK_Transactions_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Transactions_CreditCardAccounts_CreditCardAccountId" FOREIGN KEY ("CreditCardAccountId") REFERENCES "CreditCardAccounts" ("Id") ON DELETE SET NULL
);

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (1, 'Maaş', 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (2, 'Ek Gelir/Serbest', 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (3, 'Market & Mutfak', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (4, 'Kira', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (5, 'Faturalar', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (6, 'Eğlence', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (7, 'Kişisel Harçlık', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (8, 'Çocuk Harçlığı', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (9, 'Taze Gıda & Pazar', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (10, 'Aile Harcamaları', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (11, 'Ulaşım', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (12, 'Ev & Yaşam', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (13, 'Giyim', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (14, 'Eğitim', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (15, 'Sağlık', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (16, 'Hediye & Bağış', 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (17, 'Yemek Ödeneği', 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (18, 'Aile Desteği', 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (19, 'Kredi Kartı Çekilen', 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (20, 'Ekstra Ödemesi', 1);
SELECT changes();


CREATE UNIQUE INDEX "IX_BudgetLimits_CategoryId" ON "BudgetLimits" ("CategoryId");

CREATE UNIQUE INDEX "IX_InflationCaches_Year_Month" ON "InflationCaches" ("Year", "Month");

CREATE INDEX "IX_Transactions_CategoryId" ON "Transactions" ("CategoryId");

CREATE INDEX "IX_Transactions_CreditCardAccountId" ON "Transactions" ("CreditCardAccountId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221103611_InitialSQLite', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "CreditCardAccounts" ADD "PaymentDueDay" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "CreditCardAccounts" ADD "StatementDay" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221115700_AddCreditCardStatementDays', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
INSERT INTO "Categories" ("Id", "Name", "Type")
VALUES (21, 'Kredi Kartı Ödemesi', 2);
SELECT changes();


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221115946_AddTransferCategory', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "CreditCardAccounts" ADD "ParentCardId" INTEGER NULL;

CREATE INDEX "IX_CreditCardAccounts_ParentCardId" ON "CreditCardAccounts" ("ParentCardId");

CREATE TABLE "ef_temp_CreditCardAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CreditCardAccounts" PRIMARY KEY AUTOINCREMENT,
    "BankName" TEXT NOT NULL,
    "CardLabel" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "ParentCardId" INTEGER NULL,
    "PaymentDueDay" INTEGER NOT NULL,
    "StatementDay" INTEGER NOT NULL,
    CONSTRAINT "FK_CreditCardAccounts_CreditCardAccounts_ParentCardId" FOREIGN KEY ("ParentCardId") REFERENCES "CreditCardAccounts" ("Id") ON DELETE SET NULL
);

INSERT INTO "ef_temp_CreditCardAccounts" ("Id", "BankName", "CardLabel", "IsActive", "ParentCardId", "PaymentDueDay", "StatementDay")
SELECT "Id", "BankName", "CardLabel", "IsActive", "ParentCardId", "PaymentDueDay", "StatementDay"
FROM "CreditCardAccounts";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "CreditCardAccounts";

ALTER TABLE "ef_temp_CreditCardAccounts" RENAME TO "CreditCardAccounts";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_CreditCardAccounts_ParentCardId" ON "CreditCardAccounts" ("ParentCardId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221123211_AddCardHierarchy', '10.0.3');

BEGIN TRANSACTION;
ALTER TABLE "Categories" ADD "ParentCategoryId" INTEGER NULL;

UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 1;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 2;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 3;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 4;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 5;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 6;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 7;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 8;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 9;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 10;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 11;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 12;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 13;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 14;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 15;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 16;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 17;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 18;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 19;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 20;
SELECT changes();


UPDATE "Categories" SET "ParentCategoryId" = NULL
WHERE "Id" = 21;
SELECT changes();


INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (22, 'Elektrik', 5, 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (23, 'Su', 5, 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (24, 'Doğalgaz', 5, 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (25, 'İnternet & TV', 5, 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (26, 'Telefon', 5, 1);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (27, 'Aidat', 5, 1);
SELECT changes();


CREATE INDEX "IX_Categories_ParentCategoryId" ON "Categories" ("ParentCategoryId");

CREATE TABLE "ef_temp_Categories" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ParentCategoryId" INTEGER NULL,
    "Type" INTEGER NOT NULL,
    CONSTRAINT "FK_Categories_Categories_ParentCategoryId" FOREIGN KEY ("ParentCategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
);

INSERT INTO "ef_temp_Categories" ("Id", "Name", "ParentCategoryId", "Type")
SELECT "Id", "Name", "ParentCategoryId", "Type"
FROM "Categories";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "Categories";

ALTER TABLE "ef_temp_Categories" RENAME TO "Categories";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_Categories_ParentCategoryId" ON "Categories" ("ParentCategoryId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260221124626_AddCategoryHierarchy', '10.0.3');

BEGIN TRANSACTION;
ALTER TABLE "Transactions" ADD "BankAccountId" INTEGER NULL;

CREATE TABLE "BankAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_BankAccounts" PRIMARY KEY AUTOINCREMENT,
    "BankName" TEXT NOT NULL,
    "AccountName" TEXT NOT NULL,
    "IBAN" TEXT NULL,
    "InitialBalance" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE INDEX "IX_Transactions_BankAccountId" ON "Transactions" ("BankAccountId");

CREATE TABLE "ef_temp_Transactions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Transactions" PRIMARY KEY AUTOINCREMENT,
    "Amount" TEXT NOT NULL,
    "BankAccountId" INTEGER NULL,
    "CategoryId" INTEGER NOT NULL,
    "CreditCardAccountId" INTEGER NULL,
    "Date" TEXT NOT NULL,
    "Description" TEXT NULL,
    CONSTRAINT "FK_Transactions_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Transactions_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Transactions_CreditCardAccounts_CreditCardAccountId" FOREIGN KEY ("CreditCardAccountId") REFERENCES "CreditCardAccounts" ("Id") ON DELETE SET NULL
);

INSERT INTO "ef_temp_Transactions" ("Id", "Amount", "BankAccountId", "CategoryId", "CreditCardAccountId", "Date", "Description")
SELECT "Id", "Amount", "BankAccountId", "CategoryId", "CreditCardAccountId", "Date", "Description"
FROM "Transactions";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "Transactions";

ALTER TABLE "ef_temp_Transactions" RENAME TO "Transactions";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_Transactions_BankAccountId" ON "Transactions" ("BankAccountId");

CREATE INDEX "IX_Transactions_CategoryId" ON "Transactions" ("CategoryId");

CREATE INDEX "IX_Transactions_CreditCardAccountId" ON "Transactions" ("CreditCardAccountId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260222185555_AddBankAccountModel', '10.0.3');

BEGIN TRANSACTION;
CREATE TABLE "InvestmentAssets" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InvestmentAssets" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Symbol" TEXT NOT NULL,
    "Category" TEXT NULL,
    "TotalAmount" TEXT NOT NULL,
    "AverageCost" TEXT NOT NULL
);

CREATE TABLE "InvestmentTransactions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InvestmentTransactions" PRIMARY KEY AUTOINCREMENT,
    "InvestmentAssetId" INTEGER NOT NULL,
    "Type" INTEGER NOT NULL,
    "Amount" TEXT NOT NULL,
    "UnitPrice" TEXT NOT NULL,
    "TotalCost" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "Notes" TEXT NULL,
    "LinkedBankAccountId" INTEGER NULL,
    CONSTRAINT "FK_InvestmentTransactions_BankAccounts_LinkedBankAccountId" FOREIGN KEY ("LinkedBankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_InvestmentTransactions_InvestmentAssets_InvestmentAssetId" FOREIGN KEY ("InvestmentAssetId") REFERENCES "InvestmentAssets" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_InvestmentTransactions_InvestmentAssetId" ON "InvestmentTransactions" ("InvestmentAssetId");

CREATE INDEX "IX_InvestmentTransactions_LinkedBankAccountId" ON "InvestmentTransactions" ("LinkedBankAccountId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260223194907_AddInvestmentModels', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "InvestmentTransactions" ADD "LinkedCreditCardAccountId" INTEGER NULL;

CREATE INDEX "IX_InvestmentTransactions_LinkedCreditCardAccountId" ON "InvestmentTransactions" ("LinkedCreditCardAccountId");

CREATE TABLE "ef_temp_InvestmentTransactions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InvestmentTransactions" PRIMARY KEY AUTOINCREMENT,
    "Amount" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "InvestmentAssetId" INTEGER NOT NULL,
    "LinkedBankAccountId" INTEGER NULL,
    "LinkedCreditCardAccountId" INTEGER NULL,
    "Notes" TEXT NULL,
    "TotalCost" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "UnitPrice" TEXT NOT NULL,
    CONSTRAINT "FK_InvestmentTransactions_BankAccounts_LinkedBankAccountId" FOREIGN KEY ("LinkedBankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_InvestmentTransactions_CreditCardAccounts_LinkedCreditCardAccountId" FOREIGN KEY ("LinkedCreditCardAccountId") REFERENCES "CreditCardAccounts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_InvestmentTransactions_InvestmentAssets_InvestmentAssetId" FOREIGN KEY ("InvestmentAssetId") REFERENCES "InvestmentAssets" ("Id") ON DELETE CASCADE
);

INSERT INTO "ef_temp_InvestmentTransactions" ("Id", "Amount", "Date", "InvestmentAssetId", "LinkedBankAccountId", "LinkedCreditCardAccountId", "Notes", "TotalCost", "Type", "UnitPrice")
SELECT "Id", "Amount", "Date", "InvestmentAssetId", "LinkedBankAccountId", "LinkedCreditCardAccountId", "Notes", "TotalCost", "Type", "UnitPrice"
FROM "InvestmentTransactions";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "InvestmentTransactions";

ALTER TABLE "ef_temp_InvestmentTransactions" RENAME TO "InvestmentTransactions";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_InvestmentTransactions_InvestmentAssetId" ON "InvestmentTransactions" ("InvestmentAssetId");

CREATE INDEX "IX_InvestmentTransactions_LinkedBankAccountId" ON "InvestmentTransactions" ("LinkedBankAccountId");

CREATE INDEX "IX_InvestmentTransactions_LinkedCreditCardAccountId" ON "InvestmentTransactions" ("LinkedCreditCardAccountId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260223200918_AddCreditCardToInvestments', '10.0.3');

BEGIN TRANSACTION;
ALTER TABLE "InvestmentTransactions" ADD "Fee" TEXT NOT NULL DEFAULT '0.0';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260223202107_AddFeeToInvestments', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260223202247_AddFeeToInvestments_Fix', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "CreditCardAccounts" ADD "Limit" TEXT NOT NULL DEFAULT '0.0';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260308065110_AddCreditCardLimit', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "Transactions" ADD "GroupId" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260308072104_AddInstallmentGroupId', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (28, 'Hatun', NULL, 0);
SELECT changes();

INSERT INTO "Categories" ("Id", "Name", "ParentCategoryId", "Type")
VALUES (29, 'Hatun', NULL, 1);
SELECT changes();


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260312190104_AddHatunCategory', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
ALTER TABLE "Categories" ADD "IsVisible" INTEGER NOT NULL DEFAULT 0;

UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 1;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 2;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 3;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 4;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 5;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 6;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 7;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 8;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 9;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 10;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 11;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 12;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 13;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 14;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 15;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 16;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 17;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 18;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 19;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 20;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 21;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 22;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 23;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 24;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 25;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 26;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 27;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 28;
SELECT changes();


UPDATE "Categories" SET "IsVisible" = 1
WHERE "Id" = 29;
SELECT changes();


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260314064958_AddCategoryVisibility', '10.0.3');

COMMIT;

BEGIN TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260322115146_UpdateModelForOpeningBalances', '10.0.3');

COMMIT;

