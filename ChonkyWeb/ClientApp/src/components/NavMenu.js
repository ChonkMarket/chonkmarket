import React, { useState, useEffect } from 'react';
import { withRouter, matchPath } from "react-router";
import { Link, useLocation } from 'react-router-dom';
import { Collapse, Container, Navbar, NavbarBrand, NavbarToggler, NavItem } from 'reactstrap';
import { encodeDate, datePicker, isMarketOpen, tradingDayStart } from './utils/tradingDayUtil';
import './NavMenu.css';
import logo from '../assets/dark.png'
import ChartNav from './NavMenuElements/ChartNav'
import Login from './Login'

function NavMenu(props) {

  const useQuery = () => new URLSearchParams(useLocation().search)
  let symbol = useQuery().get("ticker") || 'SPY';
  let currDate = useQuery().get("date") || encodeDate(datePicker(tradingDayStart('dateObj')))

  const [activeTicker, setActiveTicker] = useState(symbol);
  const [tickers, setTickers] = useState([]);
  const [collapsed, setCollapsed] = useState(true)

  const fetchTickers = async () => {
    const response = await fetch('api/nope/tickers');
    let data = await response.json();
    data.sort();
    setTickers(data)
  }

  const toggleNavbar = () => {
    setCollapsed(!collapsed)
  }

  const handleDateChange = (date) => {
    props.history.push(`/graph/?ticker=${activeTicker}&date=${encodeDate(datePicker(date))}`)
  }

  useEffect(() => {
    fetchTickers();
  }, [])

  const chartPathMatch = matchPath(props.location.pathname, {path: "/", exact: true}) 
                      || matchPath(props.location.pathname, {path: "/graph", exact: false})

  return (
    <header>
      <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white box-shadow navbar" dark>
        <Container className="nav-container">
          <NavbarBrand className="navbar-brand" tag={Link} to="/"><img className='header-logo' src={logo} alt="logo" /></NavbarBrand>
          <NavbarToggler onClick={toggleNavbar} className="mr-2" />
          <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!collapsed} navbar style={{justifyContent: 'space-between'}}>
            <ul className="navbar-nav flex-grow" style={{marginTop: '0'}}>
              <Login />
            </ul>
            <ul className="navbar-nav flex-grow" style={{marginTop: '0'}}>
              {chartPathMatch ?
                  <ChartNav 
                  activeTicker={activeTicker} setActiveTicker={setActiveTicker} tickers={tickers} currDate={currDate} handleDateChange={handleDateChange}/>
                  :  <></>
              }
            </ul>
          </Collapse>
        </Container>
      </Navbar>
    </header>
  );
  }

NavMenu.displayName = 'NavMenu'

export default withRouter(NavMenu);