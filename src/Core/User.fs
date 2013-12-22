﻿namespace Lacjam.Core
    module User  =
        module WindowsAccount =
            open System
            open System.DirectoryServices.AccountManagement
            let getPassword() = (System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month-1) + DateTime.Now.Year.ToString())
            let changePassword newPassword = 
                use context = new PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Domain)
                use user = UserPrincipal.FindByIdentity(context, System.DirectoryServices.AccountManagement.IdentityType.SamAccountName, "cmckelt")
                let np = (System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month) + DateTime.Now.Year.ToString())
                user.ChangePassword(getPassword(), np)