# Overview
The Deployment Framework for BizTalk (BTDF) eliminates the pain associated with BizTalk application deployments, and goes far beyond BizTalk’s out-of-the-box deployment functionality. It also includes additional tools to enhance developer productivity, such as binding file management.

**The Deployment Framework for BizTalk is the single most powerful and customizable, yet _easy-to-use_ toolkit for deploying and configuring your BizTalk solutions.**

### Top Five Reasons to Use the Deployment Framework for BizTalk
1. Deploy a complex solution containing orchestrations, schemas, maps, rules, custom components, pipeline components, pipelines -- even ESB itineraries -- in *minutes*, with no human intervention
2. Eliminate ALL manual steps in your BizTalk deployments 
3. Consolidate all of your environment-specific configuration and runtime settings into one, easy-to-use Excel spreadsheet
4. Maintain a SINGLE binding file that works for all deployment environments
5. Make automated deployment a native part of the BizTalk development cycle, then use the same script to deploy to your servers
6. Bonus Reason: It's free!

**This project exists due to countless hours of volunteer effort by ONE developer at a time!  Please consider [making a donation via PayPal](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=X6A9386RAYEJY) if you find this framework useful.  Thank you in advance!**

## Features
**The Deployment Framework for BizTalk includes the following features:**
* Support for BizTalk 2006, 2006 R2, 2009, 2010 and 2013 (BizTalk 2004 is supported by Deployment Framework 4.x)
* Automation of the entire BizTalk application deployment and updating processes
* Full and Quick application update modes to reduce development cycle time
* Detailed logging for informational and troubleshooting purposes
* Visual Studio 2005/2008/2010/2012 add-in provides menu and output window integration and IntelliSense
* Support for deployment of various BizTalk artifacts including:
 * Messaging bindings
 * Orchestrations
 * Schemas
 * Maps
 * Pipelines
 * Custom components (DLL’s)
 * Custom pipeline components
 * Custom functoids
 * Rules and vocabularies
 * IIS virtual directories
 * Single Sign-On (SSO) applications
 * BAM activities and views
 * ESB Toolkit 2.x itineraries
* Configuration settings infrastructure including user-friendly settings management spreadsheet and .NET object for settings access at runtime
* SSO Settings Editor GUI for viewing and editing settings stored in SSO on the fly
* Custom ESB Toolkit 2.x SSO Resolver that pulls settings data from SSO at runtime (settings data is centrally stored in settings mgmt. spreadsheet)
* Templated bindings file that automatically targets multiple runtime environments
* Enables use of un-encoded XML for easy maintenance of adapter and port configurations in binding files
* Single deployment script serves both development workstations and production servers
* Automated packaging of entire application into standard Windows Installer MSI file
* Support for side-by-side deployment of multiple versions of a single application
* Easily customizable installation wizard for server deployments
* Deployment verification via NUnit unit test tool
* Integrated deployment of Log4Net for runtime event logging
* Automated configuration of FILE adapter directories and security
* Automated configuration of BizTalk runtime settings including debugging features and .NET assembly-to-AppDomain mappings
* Automated restart of one or more BizTalk host instances and IIS services
* Automatic addition of cross-application references
* Automatic deployment of debugging PDB files to the Global Assembly Cache (GAC)
* Support for Windows x64
* Full source code
* Infinite extensibility through open-source license

Virtually all of the features mentioned above may be selectively enabled or disabled and easily customized to meet the particular requirements of your application.

## Authors
###The Author (2008 - Today)
Thomas F. Abraham (@tfabraham), founder of IllumiTech Consulting LLC, has been the project owner and sole developer for the Deployment Framework for BizTalk since mid-2008.  He has an extensive background in software development, architecture, configuration management and systems engineering, helping to build high-performance, mission-critical applications for companies including Nasdaq, Best Buy and Wells Fargo.  Over the last 15-plus years, Thomas has worked with technologies ranging from C/C++ to BizTalk Server to Exchange and .NET, was the lead author of the book "Visual Basic .NET Solutions Toolkit" from Wrox Press and a presenter at the 2006 SOA & Business Process Conference in Redmond, WA.  He holds a number of Microsoft certifications, including MCPD, MCSD, MCT and TS for both BizTalk 2004 and 2006.  His blog is located at http://www.tfabraham.com.

###The Founder (2004 - 2008)
**Scott Colestock** created the Deployment Framework for BizTalk in 2004 and developed it through mid-2008.  He has delivered solutions in the SOA/BPM and mobile space for multiple clients in the Twin Cities, MN area.  Scott is recognized for his work with TFS, helping several clients deploy and adopt it.  He is a past BizTalk Server MVP, certified ScrumMaster, and speaks at user groups and conferences.
