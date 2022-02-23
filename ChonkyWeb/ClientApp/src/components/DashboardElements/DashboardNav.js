import React from 'react'
import { NavTitle, NavPanel, NavLink } from '@duik/it'
import { Link } from 'react-router-dom'

const DashboardNav = ({subpaths, active, url}) => {

    return (
        <div>
            <NavPanel dark style={{width: '100%'}}>
                <NavTitle>Dashboard</NavTitle>

            {subpaths.map(subpath => (
                    <NavLink 
                        leftEl={subpath.icon}
                        className={subpath.path === active ? 'active' : '' } 
                        Component={Link} to={`${url}/${subpath.path}`}
                        key={subpath.path}
                        style={{fontSize:'1rem', minHeight:'48px'}}
                    >
                        {subpath.display}
                    </NavLink>
                ))}

            </NavPanel>

        </div>
    )



}

export default DashboardNav