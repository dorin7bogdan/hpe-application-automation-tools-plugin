package com.microfocus.application.automation.tools.uft.model;

import org.apache.commons.lang.StringUtils;

import static com.microfocus.application.automation.tools.uft.utils.Constants.*;

public class UftRunAsUser {
    private String username;
    private String domain;
    private String pwd;

    public UftRunAsUser(String username, String domain, String pwd) {
        if (StringUtils.isBlank(username) ) {
            throw new IllegalArgumentException(String.format("%s is required", UFT_RUN_AS_USER));
        } else if (StringUtils.isBlank(domain)) {
            throw new IllegalArgumentException(String.format("%s is required", UFT_RUN_AS_DOMAIN));
        } if (StringUtils.isBlank(pwd)) {
            throw new IllegalArgumentException(String.format("%s is required", UFT_RUN_AS_PWD));
        }
        this.username = username;
        this.domain = domain;
        this.pwd = pwd;
    }

    public String getUsername() {
        return username;
    }

    public String getDomain() {
        return domain;
    }

    public String getPassword() {
        return pwd;
    }
}
