// =============================================================================
// Migration 006: Insert subscriptions for existing companies
//
// Run in mongosh connected to any database (uses getSiblingDB):
//   mongosh "mongodb://..." --file 006_insert_subscriptions.js
//
// Or paste in MongoDB Compass Shell.
//
// This script:
//   1. Finds all companies in BalanceLedger_Config.companies
//   2. For each company without a subscription, creates a default "Basic" subscription
//   3. Skips companies that already have a subscription
//
// Enum values:
//   SubscriptionPlan:   Basic=1, Standard=2, Premium=3, Enterprise=4
//   SubscriptionStatus: Active=1, Inactive=2, Cancelled=3, Expired=4
// =============================================================================

var configDb = db.getSiblingDB("BalanceLedger_Config");
var companies = configDb.getCollection("companies");
var subscriptions = configDb.getCollection("subscriptions");

var allCompanies = companies.find({}).toArray();
print("Companies found: " + allCompanies.length);
print("Existing subscriptions: " + subscriptions.countDocuments({}));
print("---");

var created = 0;
var skipped = 0;

allCompanies.forEach(function(company) {
    var existing = subscriptions.findOne({ CompanyId: company._id });
    if (existing) {
        print("SKIP: " + (company.Name || company._id) + " — already has subscription");
        skipped++;
        return;
    }

    var sub = {
        _id: UUID(),
        CompanyId: company._id,
        Name: "Basic",
        Plan: 1,
        Status: 1,
        Rate: NumberDecimal("2.5"),
        Limits: 1000,
        PlatformFee: NumberDecimal("0.5"),
        UsersMax: 5,
        CreatedAt: new Date(),
        ModifiedAt: null
    };

    subscriptions.insertOne(sub);
    print("OK: " + (company.Name || company._id) + " — subscription created (Basic, 5 users)");
    created++;
});

print("---");
print("Created: " + created + ", Skipped: " + skipped);
print("Total subscriptions: " + subscriptions.countDocuments({}));
