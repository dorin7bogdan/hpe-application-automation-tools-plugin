package com.microfocus.application.automation.tools.uft.model;

import hudson.util.Secret;
import org.apache.commons.lang.StringUtils;

import static com.microfocus.application.automation.tools.uft.utils.Constants.*;

public class UftRunAsUser {
    private String username;
    private String domain;
    private Secret pwd;

    private final String ARG_IS_REQUIRED = "%s is required";
    public UftRunAsUser(String username, String domain, Secret pwd) {
        if (StringUtils.isBlank(username) ) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_USER));
        } else if (StringUtils.isBlank(domain)) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_DOMAIN));
        } else if (pwd == null || StringUtils.isBlank(pwd.getPlainText()) ) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_PWD));
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

    public Secret getPassword() {
        return pwd;
    }
}
